using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la fondation de nav S0 : démarre le DevHost avec un progress.json planté
/// (ex00-hello en ARevoir), puis vérifie que (1) « / » rend le tableau de bord
/// (data-testid="dashboard"), (2) « /cours » rend la grille de modules (data-testid="module-grid"),
/// (3) la sidebar porte des pastilles (data-testid="status-dot") dont une en data-status="ARevoir",
/// (4) l'onglet primaire « nav-dashboard » pointe vers « / ». Skip propre sans Chromium.
/// Port dédié 5257 (distinct de 5247/5249/5251/5253/5255).
/// </summary>
public sealed class NavigationSmokeTests : IAsyncLifetime
{
    private const int Port = 5257;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-nav-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        var stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(_tempWorkspace);

        var progressPath = Path.Combine(stateDir, "progress.json");
        var progress = new Progress();
        progress.Exercises["ex00-hello"] = new ExerciseProgress { Status = ExerciseStatus.ARevoir, Attempts = 1 };
        new ProgressStore(progressPath).Save(progress);

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_CONTENT"] = contentDir;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = _tempWorkspace;
        psi.EnvironmentVariables["PISCINE_HOME"] = _tempHome;

        _host = Process.Start(psi)
            ?? throw new InvalidOperationException("Impossible de démarrer le DevHost.");

        await WaitForServerAsync(TimeSpan.FromSeconds(90));
    }

    public Task DisposeAsync()
    {
        if (_host is { HasExited: false })
        {
            try { _host.Kill(entireProcessTree: true); }
            catch { /* déjà mort */ }
        }
        _host?.Dispose();

        if (_tempHome is not null && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* pas critique */ }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Root_is_dashboard_and_cours_is_catalogue_with_status_dots()
    {
        using var pw = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await pw.Chromium.LaunchAsync();
        }
        catch (PlaywrightException)
        {
            return; // Chromium absent (CI sans playwright install) : skip propre.
        }

        await using (browser)
        {
            var page = await browser.NewPageAsync();

            await page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='dashboard']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            var dashHref = await page.Locator("[data-testid='nav-dashboard']").First.GetAttributeAsync("href");
            Assert.Equal("/", dashHref);

            await page.GotoAsync($"{BaseUrl}/cours", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='module-grid']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            var dots = await page.Locator("[data-testid='status-dot']").CountAsync();
            Assert.True(dots > 0, "Aucune pastille data-testid='status-dot' dans la sidebar.");

            var aRevoir = await page.Locator("[data-testid='status-dot'][data-status='ARevoir']").CountAsync();
            Assert.True(aRevoir > 0, $"Aucune pastille ARevoir trouvée (pastilles totales : {dots}).");
        }
    }

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
            catch (HttpRequestException) { /* pas prêt */ }
            catch (TaskCanceledException) { /* retente */ }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Le DevHost n'a pas répondu sur {BaseUrl} ({timeout.TotalSeconds:0}s).");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Racine du dépôt introuvable (Piscine.slnx absent).");
    }
}
