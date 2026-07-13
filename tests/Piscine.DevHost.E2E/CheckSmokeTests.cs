using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E /check : démarre le DevHost avec un workspace isolé contenant un <c>Hello.cs</c>
/// délibérément faux, pilote Chromium vers <c>/check</c>, sélectionne <c>ex00-hello</c>, clique
/// « Vérifier » et vérifie que le diff attendu/obtenu apparaît (<c>data-testid="diff-expected"</c>).
/// Prouve le câblage bout-en-bout : CheckPage → CheckService → Piscine.Grading → CheckFeedback.
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement.
/// Port dédié 5253, racine résolue via Piscine.slnx, workspace temporaire nettoyé en DisposeAsync.
/// </summary>
public sealed class CheckSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247), TerminalSmokeTests (5249), CoachingSmokeTests (5251).
    private const int Port = 5253;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");

        // Workspace temporaire isolé : on y plante un Hello.cs délibérément faux pour obtenir
        // un résultat ARevoir avec diff, quelque soit l'état de progression de l'utilisateur.
        _tempWorkspace = Path.Combine(Path.GetTempPath(), $"piscine-e2e-check-{Guid.NewGuid():N}");
        var exerciseDir = Path.Combine(_tempWorkspace, "00-setup-git", "ex00-hello");
        Directory.CreateDirectory(exerciseDir);
        await File.WriteAllTextAsync(
            Path.Combine(exerciseDir, "Hello.cs"),
            // Livrable faux : affiche "Bonjour" au lieu de "Hello, Piscine!" → ARevoir + diff.
            """
            class Hello
            {
                static void Main() => System.Console.WriteLine("Bonjour");
            }
            """);

        var contentDir = Path.Combine(repoRoot, "content");

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_CONTENT"] = contentDir;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = _tempWorkspace;

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

        // Nettoyer le workspace temporaire.
        if (_tempWorkspace is not null && Directory.Exists(_tempWorkspace))
        {
            try { Directory.Delete(_tempWorkspace, recursive: true); }
            catch { /* pas critique */ }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Wrong_deliverable_shows_diff_expected()
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
            await page.GotoAsync($"{BaseUrl}/check", new PageGotoOptions { Timeout = 30_000 });

            // Attendre que le sélecteur interactif soit prêt (HTML pré-rendu côté serveur).
            await page.WaitForSelectorAsync(
                "[data-testid='exo-select']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Workspace non initialisé → l'overlay onboarding (1ᵉʳ lancement) recouvre la page et
            // intercepterait le clic « Vérifier ». On le ferme d'abord (no-op s'il est absent).
            await OnboardingOverlay.DismissIfPresentAsync(page);

            // Sélectionner l'exercice ex00-hello.
            await page.SelectOptionAsync(
                "[data-testid='exo-select']",
                new SelectOptionValue { Value = "ex00-hello" });

            // Cliquer le bouton en boucle jusqu'à ce que le circuit Blazor Server (SignalR) ait
            // traité le clic et rendu l'état « en cours ». Le premier clic peut arriver avant que
            // le circuit interactif soit établi (pre-rendered HTML) ; les tentatives suivantes
            // atteignent le circuit une fois la connexion WebSocket montée.
            var clickDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (DateTime.UtcNow < clickDeadline)
            {
                await page.ClickAsync("[data-testid='run-check']");
                var running = await page.QuerySelectorAsync("[data-testid='check-running']");
                if (running is not null)
                {
                    break;
                }

                await Task.Delay(500);
            }

            // Vérifier que le circuit a bien reçu le clic (check-running ou check-verdict présent).
            var startSignal = await page.QuerySelectorAsync("[data-testid='check-running']")
                           ?? await page.QuerySelectorAsync("[data-testid='check-verdict']");
            if (startSignal is null)
            {
                Assert.Fail("Le circuit Blazor Server n'a pas traité le clic en 30s : ni check-running ni check-verdict présents.");
            }

            // Attendre l'affichage du verdict (la correction in-process peut prendre plusieurs
            // secondes : Roslyn compile + ProgramRunner exécute dans un ALC isolé).
            // check-running disparaît quand la correction est terminée, puis check-verdict (ou check-error) apparaît.
            IElementHandle? verdict = null;
            IElementHandle? error = null;
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(120);
            while (DateTime.UtcNow < deadline)
            {
                verdict = await page.QuerySelectorAsync("[data-testid='check-verdict']");
                error = await page.QuerySelectorAsync("[data-testid='check-error']");
                if (verdict is not null || error is not null)
                {
                    break;
                }

                await Task.Delay(1_000);
            }

            if (error is not null)
            {
                var errorText = await error.InnerTextAsync();
                Assert.Fail($"La correction a levé une exception : {errorText}");
            }

            if (verdict is null)
            {
                var pageHtml = await page.ContentAsync();
                var snippet = pageHtml.Length > 3000 ? pageHtml[..3000] : pageHtml;
                Assert.Fail(
                    $"[data-testid='check-verdict'] non trouvé après 120s. HTML (3000 chars) :\n{snippet}");
            }

            // Assertion principale : au moins un bloc diff-expected doit être rendu — preuve que
            // le moteur a comparé la sortie attendue à la sortie obtenue et l'a remonté via CheckFeedback.
            var diffCount = await page.Locator("[data-testid='diff-expected']").CountAsync();
            Assert.True(
                diffCount > 0,
                "Aucun élément data-testid='diff-expected' trouvé : le diff attendu/obtenu n'a pas été rendu.");
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
