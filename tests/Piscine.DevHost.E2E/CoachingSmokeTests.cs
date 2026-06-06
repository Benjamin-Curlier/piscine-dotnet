using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E coaching : démarre le DevHost (Blazor Server) dans un vrai processus, le pilote avec
/// Chromium, navigue vers <c>/terminal</c>, tape <c>git init</c> puis <c>git commit -m x</c> (rien
/// d'indexé) et vérifie que la carte d'indice <c>commit_nothing_staged</c> apparaît. C'est la preuve
/// bout-en-bout de TOUTE la pile : xterm → PTY → shim git → named pipe → canal → CoachingService → carte.
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement
/// plutôt que d'échouer. Port dédié (distinct des autres E2E), racine résolue via Piscine.slnx.
/// </summary>
public sealed class CoachingSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247), TerminalSmokeTests (5249) et du port de dev.
    private const int Port = 5251;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");

        _host = Process.Start(new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        }) ?? throw new InvalidOperationException("Impossible de démarrer le DevHost.");

        await WaitForServerAsync(TimeSpan.FromSeconds(60));
    }

    public Task DisposeAsync()
    {
        if (_host is { HasExited: false })
        {
            try { _host.Kill(entireProcessTree: true); }
            catch { /* le processus a déjà rendu l'âme */ }
        }
        _host?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Commit_with_nothing_staged_shows_hint_card()
    {
        using var pw = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await pw.Chromium.LaunchAsync();
        }
        catch (PlaywrightException)
        {
            // Navigateur non installé (CI sans `playwright install chromium`) : skip propre.
            // xUnit 2.x n'a pas d'API Assert.Skip ; le retour anticipé fait office de skip.
            return;
        }

        await using (browser)
        {
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/terminal", new PageGotoOptions { Timeout = 30_000 });

            // xterm est monté côté JS une fois le circuit interactif établi.
            await page.WaitForSelectorAsync(".xterm", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Le terminal doit avoir le focus pour capter les frappes : on clique sur la surface
            // de rendu (xterm route le focus vers son textarea caché).
            await page.ClickAsync(".xterm-screen");

            // 1) Initialise le dépôt (dans le dossier temp isolé de la session, géré par la page).
            await page.Keyboard.TypeAsync("git init");
            await page.Keyboard.PressAsync("Enter");

            // 2) Tente un commit sans rien indexer → doit déclencher la carte de coaching.
            await page.Keyboard.TypeAsync("git commit -m x");
            await page.Keyboard.PressAsync("Enter");

            // La carte arrive via TOUTE la pile (shim → pipe → canal → coaching → rendu Blazor).
            // 30 s pour absorber le coût de démarrage du shim + handshake du pipe + circuit SignalR.
            await page.WaitForSelectorAsync(
                "[data-hint-id='commit_nothing_staged']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Assertion explicite : la run échoue (pas un skip) si la carte n'est jamais apparue.
            var cardCount = await page.Locator("[data-hint-id='commit_nothing_staged']").CountAsync();
            Assert.True(cardCount > 0, "La carte de coaching « commit_nothing_staged » n'est pas apparue.");
        }
    }

    /// <summary>Sonde l'URL racine en boucle jusqu'à ce qu'elle réponde (ou expiration).</summary>
    private static async Task WaitForServerAsync(TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) { /* serveur pas encore prêt */ }
            catch (TaskCanceledException) { /* timeout de la requête, on retente */ }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Le DevHost n'a pas répondu sur {BaseUrl} dans le délai imparti ({timeout.TotalSeconds:0}s).");
    }

    /// <summary>Remonte depuis le dossier de sortie des tests jusqu'au dossier contenant Piscine.slnx.</summary>
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException(
                "Racine du dépôt introuvable (Piscine.slnx absent en remontant l'arborescence).");
    }
}
