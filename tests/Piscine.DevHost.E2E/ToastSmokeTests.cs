using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E du toast de push global (S4) : démarre le DevHost avec un état isolé SANS progress.json,
/// pilote Chromium vers une page <b>autre</b> que <c>/resultat</c> (<c>/cours</c>), écrit
/// <c>progress.json</c> via l'API moteur, puis vérifie que le <see cref="ToastHost"/> monté dans
/// <c>MainLayout</c> fait apparaître un toast (<c>data-testid="push-toast"</c>) <b>sans aucun clic</b>
/// — prouvant que le verdict s'affiche partout dans l'app. Skip propre sans Chromium.
/// Port dédié 5265 (distinct de 5247/5249/5251/5253/5255/5257/5259/5261).
/// </summary>
public sealed class ToastSmokeTests : IAsyncLifetime
{
    private const int Port = 5265;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;
    private string? _stateDir;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-toast-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        _stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(_stateDir);
        Directory.CreateDirectory(_tempWorkspace);

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
    public async Task Push_result_shows_global_toast_on_any_page()
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

            // 1. Naviguer vers /cours (PAS /resultat) : le ToastHost de MainLayout démarre le watcher.
            await page.GotoAsync($"{BaseUrl}/cours", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync(
                "[data-testid='module-grid']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Pas de toast au repos.
            Assert.Equal(0, await page.Locator("[data-testid='push-toast']").CountAsync());

            // 2. Écrire progress.json (même chemin surveillé) → le watcher publie le delta.
            var progress = new Progress();
            progress.Exercises["ex00-hello"] = new ExerciseProgress
            {
                Status = ExerciseStatus.ARevoir,
                Attempts = 1,
                LastAttempt = DateTimeOffset.Now,
            };
            new ProgressStore(Path.Combine(_stateDir!, "progress.json")).Save(progress);

            // 3. Sans aucun clic : le toast global apparaît (île interactive du layout).
            await page.WaitForSelectorAsync(
                "[data-testid='push-toast']",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            var entry = await page.Locator("[data-testid='toast-entry']").CountAsync();
            Assert.True(entry > 0, "Aucune entrée dans le toast de push global après écriture de progress.json.");

            var link = await page.Locator("[data-testid='toast-link']").First.GetAttributeAsync("href");
            Assert.Equal("/resultat", link);
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
