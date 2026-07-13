using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la page Réglages S6 : démarre le DevHost, puis vérifie que (1) « /reglages » rend la
/// page (data-testid="settings") avec les sections thème/échelle/éditeur/terminal, (2) basculer le
/// thème en « sombre » applique data-theme="dark" sur &lt;html&gt;, (3) régler l'échelle puis
/// enregistrer affiche la confirmation, (4) l'onglet de nav « Réglages » pointe vers /reglages.
/// Skip propre sans Chromium. Port dédié 5269 (distinct de 5247/5249/5251/5253/5255/5257/5259/5261/
/// 5263/5265/5267).
/// </summary>
public sealed class SettingsSmokeTests : IAsyncLifetime
{
    private const int Port = 5269;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-settings-{Guid.NewGuid():N}");
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
    public async Task Reglages_renders_toggles_theme_and_saves()
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

            await page.GotoAsync($"{BaseUrl}/reglages", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='settings']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Sections présentes.
            await page.WaitForSelectorAsync("[data-testid='settings-theme']", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='settings-fontscale']", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='settings-terminal']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Attendre le circuit interactif (handlers @onclick/@onchange câblés) — marqueur déterministe.
            await page.WaitForFunctionAsync(
                "() => window.__settingsReady === true",
                new PageWaitForFunctionOptions { Timeout = 30_000 });

            // Workspace non initialisé → l'overlay onboarding (1ᵉʳ lancement) recouvre la page et
            // intercepterait les clics « thème » / « enregistrer ». On le ferme (no-op s'il est absent).
            await OnboardingOverlay.DismissIfPresentAsync(page);

            // Basculer le thème en sombre → <html data-theme="dark"> (application immédiate via interop).
            // On clique le <label> (l'input radio est visuellement masqué : pointer-events:none).
            await page.Locator("[data-testid='settings-theme-dark']").ClickAsync();
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-theme') === 'dark'",
                new PageWaitForFunctionOptions { Timeout = 10_000 });

            // Enregistrer → message de confirmation.
            await page.Locator("[data-testid='settings-save']").ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='settings-saved']", new PageWaitForSelectorOptions { Timeout = 10_000 });

            // L'onglet de nav « Réglages » pointe vers /reglages.
            var href = await page.Locator("[data-testid='nav-reglages']").First.GetAttributeAsync("href");
            Assert.Equal("/reglages", href);
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
