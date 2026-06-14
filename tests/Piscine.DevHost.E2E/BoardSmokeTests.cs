using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E du tableau de bord (S1) : démarre le DevHost avec un progress.json planté (ex00-hello
/// ARevoir), va sur « / », et vérifie le board — carte « Reprendre » (data-testid="board-resume"),
/// avancement (data-testid="board-percent") et au moins une barre de module (data-testid="board-module").
/// Skip propre sans Chromium. Port dédié 5259.
/// </summary>
public sealed class BoardSmokeTests : IAsyncLifetime
{
    private const int Port = 5259;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-board-{Guid.NewGuid():N}");
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
    public async Task Board_shows_resume_progress_and_module_bars()
    {
        using var pw = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await pw.Chromium.LaunchAsync();
        }
        catch (PlaywrightException)
        {
            return; // Chromium absent : skip propre.
        }

        await using (browser)
        {
            var page = await browser.NewPageAsync();
            await page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='dashboard']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            Assert.Equal(1, await page.Locator("[data-testid='board-percent']").CountAsync());

            var resume = await page.Locator("[data-testid='board-resume']").CountAsync();
            Assert.True(resume > 0, "Carte « Reprendre » absente (ex00-hello ARevoir devrait la déclencher).");

            var bars = await page.Locator("[data-testid='board-module']").CountAsync();
            Assert.True(bars > 0, "Aucune barre de progression de module.");
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
