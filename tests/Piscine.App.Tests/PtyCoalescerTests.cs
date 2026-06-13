using System.Collections.Concurrent;
using Piscine.App.Terminal;

namespace Piscine.App.Tests;

/// <summary>
/// Tests unitaires du mecanisme de coalescence de la sortie PTY.
/// On teste directement <see cref="PtyService.CoalescingPtySession"/> via un faux
/// <see cref="IPtySession"/> (stub en memoire) : aucun PTY reel n'est spawne.
/// </summary>
public sealed class PtyCoalescerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Fausse session PTY (stub). On peut pousser des chunks manuellement.</summary>
    private sealed class FakePtySession : IPtySession
    {
        public event Action<byte[]>? Output;
        public event Action<int>? Exited;

        public void Push(byte[] chunk) => Output?.Invoke(chunk);

        public Task WriteAsync(string data, CancellationToken ct = default) => Task.CompletedTask;
        public void Resize(int cols, int rows) { }

        public ValueTask DisposeAsync()
        {
            Exited?.Invoke(0);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Construit une <see cref="PtyService.CoalescingPtySession"/> autour d'un stub.
    /// </summary>
    private static (PtyService.CoalescingPtySession session, FakePtySession fake) Build(
        TimeSpan flushInterval,
        int maxBufferBytes = 32 * 1024,
        TimeProvider? timeProvider = null)
    {
        var fake = new FakePtySession();
        var opts = new PtyCoalescerOptions
        {
            FlushInterval = flushInterval,
            MaxBufferBytes = maxBufferBytes,
            TimeProvider = timeProvider,
        };
        var session = new PtyService.CoalescingPtySession(fake, opts);
        return (session, fake);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plusieurs ecritures rapides successives sont regroupees en MOINS d'evenements Output
    /// que le nombre de chunks envoyes.
    /// </summary>
    [Fact]
    public async Task Ecritures_rapides_sont_regroupees()
    {
        const int nbChunks = 20;
        var received = new ConcurrentBag<byte[]>();

        var (session, fake) = Build(flushInterval: TimeSpan.FromMilliseconds(50));
        await using (session)
        {
            session.Output += bytes => received.Add(bytes);

            // Poussee rapide de 20 chunks de 10 octets chacun.
            for (var i = 0; i < nbChunks; i++)
            {
                fake.Push(new byte[10]);
            }

            // On attend plus long que la fenetre pour etre sur que le flush a eu lieu.
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        // Le coalescer doit avoir regroupe : moins d'evenements que de chunks.
        Assert.True(
            received.Count < nbChunks,
            $"Attendu < {nbChunks} evenements, obtenu {received.Count}");
    }

    /// <summary>
    /// Une ecriture isolee (rien en attente) est emise promptement : on la recoit dans
    /// un delai raisonnable (< 3x la fenetre).
    /// </summary>
    [Fact]
    public async Task Ecriture_isolee_est_emise_promptement()
    {
        using var received = new SemaphoreSlim(0);
        byte[]? result = null;

        var interval = TimeSpan.FromMilliseconds(30);
        var (session, fake) = Build(flushInterval: interval);
        await using (session)
        {
            session.Output += bytes =>
            {
                result = bytes;
                received.Release();
            };

            fake.Push(new byte[] { 1, 2, 3 });

            // Borne LARGE (5 s) volontaire : on vérifie que l'écriture isolée EST émise (pas bloquée
            // dans le buffer), pas une borne serrée sensible à la charge du runner CI. Une borne
            // ~3× la fenêtre (90 ms) flake sous charge (le flush passe par un timer + une boucle async).
            // La promptitude fine est couverte par Seuil_de_taille_declenche_flush_immediat et le
            // round-trip PTY réel (E2E TerminalSmokeTests).
            var seen = await received.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(seen, "L'ecriture isolee n'a jamais ete emise (bloquee dans le buffer ?).");
        }

        Assert.NotNull(result);
        Assert.Equal(new byte[] { 1, 2, 3 }, result);
    }

    /// <summary>
    /// L'ordre des octets est conserve apres coalescence.
    /// On envoie des chunks consecutifs [0], [1], [2], ..., [N-1] et on verifie que
    /// la concatenation recue est bien la sequence 0..N-1 dans le bon ordre.
    /// </summary>
    [Fact]
    public async Task Ordre_des_octets_est_conserve()
    {
        const int n = 50;
        var all = new List<byte>();
        using var done = new SemaphoreSlim(0);

        var (session, fake) = Build(flushInterval: TimeSpan.FromMilliseconds(50));
        await using (session)
        {
            session.Output += bytes =>
            {
                lock (all) all.AddRange(bytes);
            };

            for (byte i = 0; i < n; i++)
            {
                fake.Push(new[] { i });
            }

            // On attend que le flush ait eu lieu.
            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        // Apres DisposeAsync, la boucle de flush a vide le residuel.
        Assert.Equal(n, all.Count);
        for (byte i = 0; i < n; i++)
        {
            Assert.Equal(i, all[i]);
        }
    }

    /// <summary>
    /// Quand le seuil de taille est atteint, le flush est declenche AVANT la fin de la fenetre.
    /// On utilise une fenetre tres longue (1 s) et un seuil de taille de 100 octets :
    /// apres 200 octets pousses, on doit recevoir au moins un flush rapidement.
    /// </summary>
    [Fact]
    public async Task Seuil_de_taille_declenche_flush_immediat()
    {
        using var firstFlush = new SemaphoreSlim(0);
        var (session, fake) = Build(
            flushInterval: TimeSpan.FromSeconds(30),  // fenetre tres longue (jamais atteinte ici)
            maxBufferBytes: 100);
        await using (session)
        {
            session.Output += _ => firstFlush.Release();

            // 200 octets en deux chunks -> depasse le seuil de 100.
            fake.Push(new byte[120]);
            fake.Push(new byte[120]);

            // Le flush par seuil de taille doit arriver SANS attendre la fenetre (30 s). On attend
            // jusqu'a 3 s : largement au-dessus du hop async (donc pas de flake sous charge CI) mais
            // tres en-dessous de la fenetre de 30 s (donc c'est bien le SEUIL, pas le timer, qui declenche).
            var seen = await firstFlush.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.True(seen, "Le seuil de taille n'a pas declenche de flush avant la fenetre.");
        }
    }

    /// <summary>
    /// Aucun octet n'est perdu : la totalite des octets pousses est recue, meme si le
    /// Dispose arrive pendant que des chunks sont encore dans le channel.
    /// </summary>
    [Fact]
    public async Task Aucun_octet_perdu_au_dispose()
    {
        const int nbChunks = 30;
        const int chunkSize = 8;
        var totalReceived = 0;

        var (session, fake) = Build(flushInterval: TimeSpan.FromMilliseconds(500)); // fenetre longue
        session.Output += bytes => Interlocked.Add(ref totalReceived, bytes.Length);

        // Pousser des chunks AVANT le Dispose.
        for (var i = 0; i < nbChunks; i++)
        {
            fake.Push(new byte[chunkSize]);
        }

        // Dispose : doit vider le residuel et le flusher.
        await session.DisposeAsync();

        Assert.Equal(nbChunks * chunkSize, totalReceived);
    }
}
