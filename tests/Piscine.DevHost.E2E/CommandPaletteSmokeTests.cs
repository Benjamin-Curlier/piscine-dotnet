using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la palette de commande S3 : démarre le DevHost, ouvre la palette via ⌘K/Ctrl+K,
/// filtre sur « Progression », sélectionne le résultat et vérifie que l'app navigue vers /progress.
/// Skip propre sans Chromium. Port dédié 5263 (distinct de 5247/5249/5251/5253/5255/5257/5259/5261).
/// </summary>
public sealed class CommandPaletteSmokeTests : IAsyncLifetime
{
    private const int Port = 5263;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-cmdk-{Guid.NewGuid():N}");
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
    public async Task CtrlK_opens_palette_and_selecting_a_result_navigates()
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

            // Le hotkey global est enregistré au 1er rendu interactif de la palette (circuit Blazor) :
            // le module JS pose window.__cmdkReady une fois le handler keydown attaché. On attend ce
            // marqueur déterministe (pas seulement window.Blazor, vrai bien avant que le circuit monte).
            await page.WaitForFunctionAsync(
                "() => window.__cmdkReady === true",
                new PageWaitForFunctionOptions { Timeout = 30_000 });

            // S'assurer que le document a le focus pour que le keydown global soit délivré.
            await page.ClickAsync("body");

            // Ouvrir la palette via le raccourci global.
            await page.Keyboard.PressAsync("Control+k");
            await page.WaitForSelectorAsync("[data-testid='command-palette']", new PageWaitForSelectorOptions { Timeout = 10_000 });

            // Filtrer puis sélectionner « Progression ».
            await page.FillAsync("[data-testid='command-palette-input']", "Progression");
            await page.WaitForSelectorAsync("[data-testid='cmd-nav-progress']", new PageWaitForSelectorOptions { Timeout = 10_000 });
            await page.ClickAsync("[data-testid='cmd-nav-progress']");

            // L'app a navigué vers /progress.
            await page.WaitForURLAsync($"{BaseUrl}/progress", new PageWaitForURLOptions { Timeout = 10_000 });
            Assert.EndsWith("/progress", page.Url, StringComparison.Ordinal);
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
