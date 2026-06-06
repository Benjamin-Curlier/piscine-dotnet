using System.Text;
using Porta.Pty;

namespace Piscine.App.Terminal;

/// <summary>Lance des sessions PTY (un vrai shell OS). Enregistre en DI, sans etat partage.</summary>
public sealed class PtyService
{
    public async Task<IPtySession> StartAsync(PtyStartInfo info, CancellationToken ct = default)
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
        return new PtySession(connection);
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
}
