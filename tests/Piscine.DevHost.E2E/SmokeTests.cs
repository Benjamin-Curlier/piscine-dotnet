using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E : démarre le DevHost (Blazor Server) dans un vrai processus, le pilote avec un
/// navigateur Chromium réel et vérifie qu'un cours se rend (titre + &lt;h1&gt;).
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement
/// plutôt que d'échouer — la run solution reste verte.
/// </summary>
public sealed class SmokeTests : IAsyncLifetime
{
    // Port dédié pour éviter tout conflit avec le port de dev par défaut (5244).
    private const int Port = 5247;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;

    public async ValueTask InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");

        _host = Process.Start(new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        }) ?? throw new InvalidOperationException("Impossible de démarrer le DevHost.");

        // Kestrel + premier build de `dotnet run` peuvent être lents : on sonde l'URL en boucle
        // (jusqu'à ~60 s) au lieu d'un délai fixe.
        await WaitForServerAsync(TimeSpan.FromSeconds(60));
    }

    public ValueTask DisposeAsync()
    {
        if (_host is { HasExited: false })
        {
            try { _host.Kill(entireProcessTree: true); }
            catch { /* le processus a déjà rendu l'âme */ }
        }
        _host?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DevHost_renders_a_course()
    {
        using var pw = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await pw.Chromium.LaunchAsync();
        }
        catch (PlaywrightException)
        {
            Assert.Skip("Chromium Playwright n'est pas installé.");
            return;
        }

        await using (browser)
        {
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/cours", new PageGotoOptions { Timeout = 30_000 });
            await OnboardingOverlay.DismissIfPresentAsync(page);

            // Le catalogue de cours expose la grille de modules + un titre.
            await page.WaitForSelectorAsync("[data-testid='module-grid']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            Assert.Contains("Piscine", await page.TitleAsync());
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
