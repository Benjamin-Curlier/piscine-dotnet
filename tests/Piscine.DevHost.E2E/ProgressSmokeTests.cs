using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E /progress : démarre le DevHost avec un état isolé contenant un <c>progress.json</c>
/// planté (un exercice <c>ARevoir</c>), pilote Chromium vers <c>/progress</c>, vérifie que l'arbre
/// modules/exos s'affiche (<c>data-testid="progress-list"</c>) et qu'au moins un badge porte le
/// statut planté (<c>data-status="ARevoir"</c>), ainsi qu'un lien de cours (<c>exo-course-link</c>).
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement.
/// Port dédié 5255, racine résolue via Piscine.slnx, répertoires temporaires nettoyés en DisposeAsync.
/// </summary>
public sealed class ProgressSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247), TerminalSmokeTests (5249), CoachingSmokeTests (5251),
    // CheckSmokeTests (5253).
    private const int Port = 5255;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        // Répertoires temporaires isolés pour cet E2E.
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-progress-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        var stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(_tempWorkspace);

        // Planter un progress.json avec ex00-hello en statut ARevoir.
        // Format exact de ProgressStore : JSON camelCase, enum en chaîne.
        var progressPath = Path.Combine(stateDir, "progress.json");
        await File.WriteAllTextAsync(progressPath, """
            {
              "exercises": {
                "ex00-hello": {
                  "status": "ARevoir",
                  "attempts": 1,
                  "lastAttempt": null
                }
              }
            }
            """);

        // Le DevHost lit l'état depuis PISCINE_HOME/.state/progress.json
        // (PiscineLayout : stateDir = home/.state, ProgressPath = stateDir/progress.json).
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
            catch { /* le processus a déjà rendu l'âme */ }
        }
        _host?.Dispose();

        // Nettoyer les répertoires temporaires.
        if (_tempHome is not null && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* pas critique */ }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Progress_page_shows_status_badges_including_planted_ARevoir()
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
            await page.GotoAsync($"{BaseUrl}/progress", new PageGotoOptions { Timeout = 30_000 });

            // Attendre que le composant interactif soit rendu (SignalR circuit établi).
            await page.WaitForSelectorAsync(
                "[data-testid='progress-list']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Au moins un badge de statut doit être présent.
            var badgeCount = await page.Locator("[data-testid='status-badge']").CountAsync();
            Assert.True(badgeCount > 0, "Aucun data-testid='status-badge' trouvé sur /progress.");

            // Le badge planté (ARevoir) doit apparaître.
            var aRevoirBadges = await page.Locator("[data-testid='status-badge'][data-status='ARevoir']").CountAsync();
            Assert.True(
                aRevoirBadges > 0,
                $"Aucun badge data-status='ARevoir' trouvé. Badges totaux : {badgeCount}. " +
                "Vérifier que PISCINE_HOME est bien transmis au DevHost et que progress.json est lu.");

            // Un lien vers la page de cours doit être présent.
            var courseLinks = await page.Locator("[data-testid='exo-course-link']").CountAsync();
            Assert.True(courseLinks > 0, "Aucun data-testid='exo-course-link' trouvé sur /progress.");
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
