using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E /resultat : démarre le DevHost avec un état isolé sans résultat pré-écrit,
/// pilote Chromium vers <c>/resultat</c>, vérifie le placeholder vide
/// (<c>data-testid="push-empty"</c>), puis écrit l'artefact canonique du hook et
/// vérifie que la page se met à jour <b>sans aucun clic</b> (auto-refresh FSW + SignalR).
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement.
/// Port dédié 5261. Racine résolue via Piscine.slnx. Répertoires temporaires nettoyés en DisposeAsync.
/// </summary>
public sealed class PushResultSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de tous les autres smoke tests.
    private const int Port = 5261;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;
    private string? _stateDir;

    public async ValueTask InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        // Répertoires temporaires isolés — pas de progress.json pré-écrit.
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-resultat-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        _stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(_stateDir);
        Directory.CreateDirectory(_tempWorkspace);

        // Démarrer le DevHost avec le même PISCINE_HOME que _stateDir = home/.state.
        // Le ProgressFileWatcher surveille exactement ce dossier (PiscineLayout.StateDir).
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

    public ValueTask DisposeAsync()
    {
        if (_host is { HasExited: false })
        {
            try { _host.Kill(entireProcessTree: true); }
            catch { /* processus déjà terminé */ }
        }
        _host?.Dispose();

        if (_tempHome is not null && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* pas critique */ }
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Push_result_renders_without_manual_action()
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

            // 1. Naviguer vers /resultat — le composant démarre le watcher et prend un snapshot
            //    initial (fichier absent → état vide). Attendre le placeholder.
            await page.GotoAsync($"{BaseUrl}/resultat", new PageGotoOptions { Timeout = 30_000 });
            await OnboardingOverlay.DismissIfPresentAsync(page);
            await page.WaitForSelectorAsync(
                "[data-testid='push-empty']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // 2. Persister d'abord la progression, puis le document du push qui déclenche le watcher.
            var progress = new Progress();
            progress.Exercises["ex00-hello"] = new ExerciseProgress
            {
                Status = ExerciseStatus.ARevoir,
                Attempts = 1,
                LastAttempt = DateTimeOffset.Now,
            };
            new ProgressStore(Path.Combine(_stateDir!, "progress.json")).Save(progress);
            new LastPushResultStore(Path.Combine(_stateDir!, "last-push-result.json")).Save(
                new PushResultDocument(
                    new[]
                    {
                        new PushExerciseResult(
                            "ex00-hello", "00-setup-git", "ARevoir",
                            new[] { new PushCaseResult("io", false, new[] { "Échec du cas." }) },
                            Hint: null,
                            CourseRef: null)
                        {
                            Attempts = 1,
                            LastAttempt = progress.Exercises["ex00-hello"].LastAttempt,
                        },
                    },
                    DateTimeOffset.Now)
                {
                    PushId = Guid.NewGuid().ToString("N"),
                });

            // 3. Sans aucun clic : attendre l'apparition automatique du premier push-entry.
            //    Timeout généreux pour absorber debounce (250 ms) + latence SignalR.
            await page.WaitForSelectorAsync(
                "[data-testid='push-entry']",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            // 4. Vérifier le badge de statut.
            var statusBadge = await page.Locator(
                "[data-testid='status-badge'][data-status='ARevoir']").CountAsync();
            Assert.True(
                statusBadge > 0,
                "Aucun [data-testid='status-badge'][data-status='ARevoir'] après écriture du résultat. " +
                "Vérifier l'alignement PISCINE_HOME / _stateDir et que le FSW surveille bien le bon chemin.");

            // Le document riche corrélé est rendu, sans relecture indépendante de progress.json.
            Assert.True(await page.Locator("[data-testid='check-verdict']").CountAsync() > 0);
        }
    }

    [Fact]
    public async Task Rich_push_result_renders_diff_inline_without_manual_action()
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

            await page.GotoAsync($"{BaseUrl}/resultat", new PageGotoOptions { Timeout = 30_000 });
            await OnboardingOverlay.DismissIfPresentAsync(page);
            await page.WaitForSelectorAsync(
                "[data-testid='push-empty']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Écrire D'ABORD la progression, PUIS l'artefact riche canonique qui déclenche le watcher.
            var doc = new PushResultDocument(
                new[]
                {
                    new PushExerciseResult(
                        "ex00-hello", "00-setup", "ARevoir",
                        new[] { new PushCaseResult("io", false, new[] { "Attendu : ok", "Obtenu  : non" }) },
                        "Relis l'énoncé.", "cours.md#hello"),
                },
                DateTimeOffset.Now);
            var progress = new Progress();
            progress.Exercises["ex00-hello"] = new ExerciseProgress
            {
                Status = ExerciseStatus.ARevoir,
                Attempts = 1,
                LastAttempt = DateTimeOffset.Now,
            };
            new ProgressStore(Path.Combine(_stateDir!, "progress.json")).Save(progress);
            doc = doc with
            {
                PushId = Guid.NewGuid().ToString("N"),
                Exercises = doc.Exercises.Select(exercise => exercise with
                {
                    Attempts = 1,
                    LastAttempt = progress.Exercises["ex00-hello"].LastAttempt,
                }).ToList(),
            };
            new LastPushResultStore(Path.Combine(_stateDir!, "last-push-result.json")).Save(doc);

            // Sans aucun clic : le diff riche (CheckFeedback) apparaît, pas le lien /check.
            await page.WaitForSelectorAsync(
                "[data-testid='diff-actual']",
                new PageWaitForSelectorOptions { Timeout = 15_000 });

            Assert.True(
                await page.Locator("[data-testid='diff-expected']").CountAsync() > 0,
                "Le diff attendu (#40) n'est pas rendu inline sur /resultat.");
            Assert.Equal(0, await page.Locator("[data-testid='push-check-link']").CountAsync());
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
