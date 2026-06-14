using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de l'onboarding 1ᵉʳ lancement (S7) : démarre le DevHost avec un <c>PISCINE_HOME</c> temp
/// vierge (workspace NON initialisé), charge « / » et vérifie que l'overlay d'onboarding
/// (<c>data-testid="onboarding"</c>) s'affiche sur l'écran de bienvenue, puis pilote le parcours
/// Bienvenue → Init → Fait, vérifie le CTA vers le 1ᵉʳ exercice, et enfin qu'après initialisation un
/// rechargement de « / » ne réaffiche PLUS l'overlay (pas de harcèlement).
/// Skip propre sans Chromium (CI sans <c>playwright install</c>). Port dédié 5271, racine via Piscine.slnx.
/// </summary>
public sealed class OnboardingSmokeTests : IAsyncLifetime
{
    // Port dédié unique pour ce fichier (distinct des autres E2E : 5247/5249/.../5269).
    private const int Port = 5271;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        // Poste vierge, NON pré-initialisé : on crée _tempHome mais pas workspace/.state.
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-onboarding-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        Directory.CreateDirectory(_tempHome);

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_CONTENT"] = contentDir;
        psi.EnvironmentVariables["PISCINE_HOME"] = _tempHome;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = _tempWorkspace;
        psi.EnvironmentVariables["PISCINE_EXE"] = "piscine";

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
            try
            {
                ClearReadOnly(_tempHome);
                Directory.Delete(_tempHome, recursive: true);
            }
            catch { /* pas critique */ }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Onboarding_guides_init_then_does_not_nag_after()
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

            // L'overlay (île interactive) s'affiche sur l'écran de bienvenue quand non initialisé.
            await page.WaitForSelectorAsync(
                "[data-testid='onboarding-welcome']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Attendre le circuit interactif (handlers @onclick câblés) — marqueur déterministe.
            await page.WaitForFunctionAsync(
                "() => window.__onboardingReady === true",
                new PageWaitForFunctionOptions { Timeout = 30_000 });

            // Étape 1 → 2 : « Commencer ». Le 1ᵉʳ clic après montage du circuit demande un délai
            // d'actionnabilité généreux ; ensuite on attend l'apparition de l'écran d'init.
            await page.ClickAsync("[data-testid='onboarding-start']", new PageClickOptions { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='onboarding-init']", new PageWaitForSelectorOptions { Timeout = 15_000 });

            // Étape 2 → 3 : « Initialiser maintenant » → écran « Fait » (init crée bare + hook).
            await page.ClickAsync("[data-testid='onboarding-run-init']", new PageClickOptions { Timeout = 15_000 });
            await page.WaitForSelectorAsync("[data-testid='onboarding-done']", new PageWaitForSelectorOptions { Timeout = 60_000 });

            // CTA présent : pointe vers un exercice réel du curriculum (/module/...).
            var cta = await page.QuerySelectorAsync("[data-testid='onboarding-first-exercise']")
                ?? throw new InvalidOperationException("CTA 1ᵉʳ exercice introuvable.");
            var href = await cta.GetAttributeAsync("href");
            Assert.False(string.IsNullOrWhiteSpace(href));
            Assert.StartsWith("/module/", href!, StringComparison.Ordinal);

            // Preuve FS : le hook post-receive existe dans le home temporaire isolé.
            var expectedHook = Path.Combine(_tempHome!, ".state", "remote.git", "hooks", "post-receive");
            Assert.True(File.Exists(expectedHook), $"Hook post-receive absent : {expectedHook}.");

            // Pas de harcèlement : recharger « / » après init → plus d'overlay.
            await page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='dashboard']", new PageWaitForSelectorOptions { Timeout = 30_000 });
            // Laisser le circuit interactif monter, puis vérifier l'absence d'overlay.
            await page.WaitForFunctionAsync(
                "!document.querySelector('[data-testid=\"onboarding\"]')",
                null,
                new PageWaitForFunctionOptions { Timeout = 15_000 });
            Assert.Equal(0, await page.Locator("[data-testid='onboarding']").CountAsync());
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

    private static void ClearReadOnly(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }
}
