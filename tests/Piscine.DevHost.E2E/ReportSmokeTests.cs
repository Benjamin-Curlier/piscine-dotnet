using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la page de rapport S5 : démarre le DevHost avec un progress.json planté
/// (ex00-hello en ARevoir), puis vérifie que (1) « /rapport » rend la page
/// (data-testid="report") avec l'en-tête d'identité et l'avancement, (2) le tableau par module
/// est présent, (3) le bouton « Copier en Markdown » est rendu. Skip propre sans Chromium.
/// Port dédié 5267 (distinct de 5247/5249/5251/5253/5255/5257/5259/5261).
/// </summary>
public sealed class ReportSmokeTests : IAsyncLifetime
{
    private const int Port = 5267;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-report-{Guid.NewGuid():N}");
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
    public async Task Rapport_renders_with_progress_table_and_export_button()
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

            await page.GotoAsync($"{BaseUrl}/rapport", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='report']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // En-tête identité + avancement global.
            await page.WaitForSelectorAsync("[data-testid='report-identity']", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='report-percent']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Tableau par module.
            var rows = await page.Locator("[data-testid='report-module-row']").CountAsync();
            Assert.True(rows > 0, "Aucune ligne de module dans le tableau de rapport.");

            // Bouton d'export Markdown présent.
            var copyBtn = await page.Locator("[data-testid='report-copy-md']").CountAsync();
            Assert.True(copyBtn > 0, "Bouton « Copier en Markdown » absent.");

            // L'onglet de nav « Rapport » pointe vers /rapport.
            var href = await page.Locator("[data-testid='nav-rapport']").First.GetAttributeAsync("href");
            Assert.Equal("/rapport", href);
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
