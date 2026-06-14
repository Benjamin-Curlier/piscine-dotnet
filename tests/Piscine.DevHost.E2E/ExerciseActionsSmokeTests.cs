using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la barre d'action S2 : démarre le DevHost, navigue vers un exercice, et vérifie
/// que la barre d'action (exercise-actions) est présente avec les boutons Ouvrir et le lien Vérifier,
/// et que l'ancienne notice « piscine start » a disparu.
/// Port dédié 5261 (distinct de 5247/5249/5251/5253/5255/5257/5259).
/// </summary>
public sealed class ExerciseActionsSmokeTests : IAsyncLifetime
{
    private const int Port = 5261;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-actions-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        var stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(stateDir);
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
    public async Task ExercisePage_has_action_bar_and_no_piscine_start_notice()
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

            await page.GotoAsync($"{BaseUrl}/module/00-setup-git/ex00-hello", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='exercise-actions']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            var openFolderCount = await page.Locator("[data-testid='ex-open-folder']").CountAsync();
            Assert.True(openFolderCount > 0, "Le bouton 'Ouvrir le dossier' (data-testid='ex-open-folder') est absent.");

            var checkCount = await page.Locator("[data-testid='ex-check']").CountAsync();
            Assert.True(checkCount > 0, "Le lien 'Vérifier' (data-testid='ex-check') est absent.");

            Assert.DoesNotContain("piscine start", await page.ContentAsync(), StringComparison.Ordinal);
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
