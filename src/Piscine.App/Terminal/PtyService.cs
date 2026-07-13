using System.Text;
using System.Threading.Channels;
using Porta.Pty;

namespace Piscine.App.Terminal;

/// <summary>
/// Lance des sessions PTY (un vrai shell OS). Enregistre en DI, sans etat partage.
/// <para>
/// Si des <see cref="PtyCoalescerOptions"/> sont fournies a <see cref="StartAsync"/>, la sortie brute
/// est bufferisee et emise par rafales (coalescing) afin d'eviter de saturer le pont JS/interop sur
/// une sortie tres verbeuse. Sans options, la session est directe (comportement historique S2).
/// </para>
/// </summary>
public sealed class PtyService
{
    /// <summary>
    /// Lance une session PTY.
    /// </summary>
    /// <param name="info">Parametres de demarrage du shell.</param>
    /// <param name="coalescer">
    /// Options de coalescence de la sortie (fenetre temporelle + seuil de taille).
    /// <c>null</c> = emission directe sans buffer (comportement historique).
    /// </param>
    /// <param name="ct">Jeton d'annulation de l'operation de spawn.</param>
    public async Task<IPtySession> StartAsync(
        PtyStartInfo info,
        PtyCoalescerOptions? coalescer = null,
        CancellationToken ct = default)
    {
        var options = new PtyOptions
        {
            Name = "piscine-term",
            App = info.Shell ?? PtyStartInfo.DefaultShell(),
            Cwd = info.WorkingDirectory,
            Cols = info.Cols,
            Rows = info.Rows,
        };

        // Surcharges d'env (shim git) : on part de l'env courant puis on applique les overrides du
        // caller (PATH prefixe du dossier shim, PISCINE_REAL_GIT, PISCINE_COACH_PIPE). Si aucune
        // surcharge n'est demandee, on laisse l'env par defaut du PTY (comportement S2 inchange).
        if (info.Environment is { Count: > 0 })
        {
            options.Environment = BuildEnvironment(info.Environment);
        }

        var connection = await PtyProvider.SpawnAsync(options, ct);
        IPtySession raw = new PtySession(connection);

        if (coalescer is null)
        {
            return raw;
        }

        return new CoalescingPtySession(raw, coalescer);
    }

    /// <summary>
    /// Construit l'env de la session : copie de l'env du processus courant, puis surcharges du caller.
    /// Les surcharges ecrasent les cles existantes (insensible a la casse pour gerer PATH/Path sous
    /// Windows : on ne cree pas de cle PATH dupliquee).
    /// </summary>
    private static IDictionary<string, string> BuildEnvironment(IReadOnlyDictionary<string, string> overrides)
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                env[key] = value;
            }
        }

        foreach (var (key, value) in overrides)
        {
            env[key] = value;
        }

        return env;
    }

    // ── Session PTY directe (comportement historique S2) ────────────────────────

    private sealed class PtySession : IPtySession
    {
        private readonly IPtyConnection _conn;
        private readonly CancellationTokenSource _cts = new();

        public event Action<byte[]>? Output;
        public event Action<int>? Exited;

        public PtySession(IPtyConnection conn)
        {
            _conn = conn;
            _conn.ProcessExited += (_, e) => Exited?.Invoke(e.ExitCode);
            _ = PumpAsync(_cts.Token);
        }

        private async Task PumpAsync(CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var n = await _conn.ReaderStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
                    if (n <= 0)
                    {
                        break;
                    }

                    var chunk = new byte[n];
                    Array.Copy(buffer, chunk, n);
                    Output?.Invoke(chunk);
                }
            }
            catch (OperationCanceledException)
            {
                // Arret normal (DisposeAsync a annule le pump).
            }
            catch (IOException)
            {
                // PTY ferme cote shell (processus mort) : fin de flux silencieuse.
            }
        }

        public async Task WriteAsync(string data, CancellationToken ct = default)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            await _conn.WriterStream.WriteAsync(bytes.AsMemory(), ct);
            await _conn.WriterStream.FlushAsync(ct);
        }

        public void Resize(int cols, int rows) => _conn.Resize(cols, rows);

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            try
            {
                _conn.Kill();
            }
            catch (Exception)
            {
                // Le shell peut deja etre mort : la terminaison est idempotente cote spike.
            }

            _conn.Dispose();
            _cts.Dispose();
        }
    }

    // ── Session PTY avec coalescence de la sortie ────────────────────────────────

    /// <summary>
    /// Enveloppe une <see cref="IPtySession"/> brute et coalesce sa sortie.
    /// <para>
    /// Architecture : les chunks entrants (thread PTY) sont enfiles dans un <see cref="Channel{T}"/>.
    /// Un timer periodique (pilote par <see cref="PtyCoalescerOptions.TimeProvider"/>) declenche les
    /// flush ; le seuil de taille provoque un flush immediat supplementaire entre deux ticks.
    /// </para>
    /// <para>
    /// Le flush est declenche par l'un ou l'autre des criteres suivants (premier atteint) :
    /// <list type="bullet">
    ///   <item>le timer de <see cref="PtyCoalescerOptions.FlushInterval"/> se declenche ;</item>
    ///   <item>le buffer accumule <see cref="PtyCoalescerOptions.MaxBufferBytes"/> octets.</item>
    /// </list>
    /// </para>
    /// <para>
    /// L'ordre des octets est preserve. Aucun octet n'est perdu (Channel non borne).
    /// </para>
    /// </summary>
    internal sealed class CoalescingPtySession : IPtySession
    {
        private readonly IPtySession _inner;
        private readonly PtyCoalescerOptions _options;
        private readonly Channel<byte[]> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _flushLoop;
        private readonly ITimer _flushTimer;

        // Semaphore utilise comme signal de flush :
        // - le timer periodique le libere a chaque tick (fenetre temporelle) ;
        // - OnInnerOutput le libere quand le seuil de taille est atteint (flush immediat).
        private readonly SemaphoreSlim _flushSignal = new(0);

        // Taille totale accumulee dans le channel depuis le dernier flush : utilisee pour decider si le
        // seuil de taille est atteint. Ecrit par DEUX threads en concurrence — le thread PTY
        // (OnInnerOutput, ajout du chunk) et le thread de flush (RunFlushLoopAsync, remise a zero) —
        // donc tous les acces passent par Interlocked (atomicite + visibilite inter-thread). La valeur
        // exacte n'est pas critique : le Channel reste la source de verite des octets ; ce compteur ne
        // sert qu'a decider d'un flush anticipe sur seuil.
        private int _pendingBytes;

        public event Action<byte[]>? Output;
        public event Action<int>? Exited;

        internal CoalescingPtySession(IPtySession inner, PtyCoalescerOptions options)
        {
            _inner = inner;
            _options = options;
            _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });

            _inner.Output += OnInnerOutput;
            _inner.Exited += exitCode => Exited?.Invoke(exitCode);

            // Cree un timer periodique via TimeProvider (injectable : production = systeme,
            // tests = FakeTimeProvider de Microsoft.Extensions.TimeProvider.Testing).
            // Le timer libere _flushSignal a chaque tick ; la boucle de flush se reveille alors
            // et vide le channel. On stocke l'ITimer pour eviter qu'il soit GC avant DisposeAsync.
            var tp = options.TimeProvider ?? TimeProvider.System;
            _flushTimer = tp.CreateTimer(
                _ => _flushSignal.Release(),
                state: null,
                dueTime: options.FlushInterval,
                period: options.FlushInterval);

            _flushLoop = RunFlushLoopAsync(_cts.Token);
        }

        private void OnInnerOutput(byte[] chunk)
        {
            _channel.Writer.TryWrite(chunk);

            // Seuil de taille : si le buffer accumule depasse MaxBufferBytes, on signale un flush
            // immediat sans attendre le prochain tick du timer. Interlocked car la boucle de flush
            // remet _pendingBytes a zero en concurrence (cf. commentaire du champ).
            var total = Interlocked.Add(ref _pendingBytes, chunk.Length);
            if (total >= _options.MaxBufferBytes)
            {
                Interlocked.Exchange(ref _pendingBytes, 0);
                _flushSignal.Release();
            }
        }

        private async Task RunFlushLoopAsync(CancellationToken ct)
        {
            var pending = new List<byte[]>();
            var pendingSize = 0;

            while (!ct.IsCancellationRequested)
            {
                // Attendre le signal de flush (timer periodique ou seuil de taille).
                try
                {
                    await _flushSignal.WaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // Arret demande (DisposeAsync) : vider le reste et sortir.
                    DrainAndFlush(pending, ref pendingSize);
                    break;
                }

                // Vider tout ce qui est disponible dans le channel au moment du flush.
                Interlocked.Exchange(ref _pendingBytes, 0); // RAZ (Interlocked : concurrent avec OnInnerOutput).
                while (_channel.Reader.TryRead(out var chunk))
                {
                    pending.Add(chunk);
                    pendingSize += chunk.Length;
                }

                Flush(pending, ref pendingSize);
            }
        }

        /// <summary>Fusionne <paramref name="pending"/> en un seul tableau et invoque <see cref="Output"/>.</summary>
        private void Flush(List<byte[]> pending, ref int pendingSize)
        {
            if (pending.Count == 0) return;

            byte[] merged;
            if (pending.Count == 1)
            {
                merged = pending[0];
            }
            else
            {
                merged = new byte[pendingSize];
                var offset = 0;
                foreach (var seg in pending)
                {
                    Buffer.BlockCopy(seg, 0, merged, offset, seg.Length);
                    offset += seg.Length;
                }
            }

            pending.Clear();
            pendingSize = 0;

            Output?.Invoke(merged);
        }

        /// <summary>Vide le channel residuel apres annulation, puis flush une derniere fois.</summary>
        private void DrainAndFlush(List<byte[]> pending, ref int pendingSize)
        {
            while (_channel.Reader.TryRead(out var chunk))
            {
                pending.Add(chunk);
                pendingSize += chunk.Length;
            }
            Flush(pending, ref pendingSize);
        }

        public Task WriteAsync(string data, CancellationToken ct = default) => _inner.WriteAsync(data, ct);
        public void Resize(int cols, int rows) => _inner.Resize(cols, rows);

        public async ValueTask DisposeAsync()
        {
            _inner.Output -= OnInnerOutput;

            // Arreter le timer periodique.
            await _flushTimer.DisposeAsync();

            // Annuler la boucle de flush.
            await _cts.CancelAsync();
            try
            {
                await _flushLoop;
            }
            catch (OperationCanceledException)
            {
                // Attendu lors de l'arret.
            }

            _cts.Dispose();
            _flushSignal.Dispose();
            await _inner.DisposeAsync();
        }
    }
}
