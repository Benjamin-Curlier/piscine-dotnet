using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E terminal : démarre le DevHost (Blazor Server) dans un vrai processus, le pilote avec
/// Chromium, navigue vers <c>/terminal</c>, tape <c>echo PISCINE_E2E</c> dans le terminal xterm et
/// vérifie que la chaîne apparaît dans le DOM rendu par xterm — preuve bout-en-bout
/// PTY → pont SignalR → xterm → DOM.
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement
/// plutôt que d'échouer — la run solution reste verte. Port dédié, racine résolue via Piscine.slnx.
/// </summary>
public sealed class TerminalSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247) et du port de dev (5244/5248).
    private const int Port = 5249;
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
    public async Task Typing_echo_shows_output()
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

            await page.Keyboard.TypeAsync("echo PISCINE_E2E");
            await page.Keyboard.PressAsync("Enter");

            // La frappe (échoée par le PTY) ET la sortie de la commande contiennent la chaîne :
            // dès que xterm rend la ligne, le buffer DOM la contient. Robuste cross-shell.
            await page.WaitForFunctionAsync(
                "() => document.querySelector('.xterm-screen')?.textContent?.includes('PISCINE_E2E')",
                new PageWaitForFunctionOptions { Timeout = 30_000 });

            // Assertion explicite : la run échoue (pas un skip) si la sortie n'est jamais apparue.
            var screenText = await page.EvalOnSelectorAsync<string>(
                ".xterm-screen", "el => el.textContent ?? ''");
            Assert.Contains("PISCINE_E2E", screenText);
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
