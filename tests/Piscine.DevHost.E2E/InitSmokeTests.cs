using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E /init : démarre le DevHost avec un <c>PISCINE_HOME</c> temp vierge (non initialisé),
/// pilote Chromium vers <c>/init</c>, vérifie que la page affiche « Non initialisé »
/// (<c>data-initialized="False"</c>), clique « Initialiser » et vérifie le succès
/// (<c>data-success="True"</c>, <c>data-initialized="True"</c>). Puis re-clique pour prouver
/// l'idempotence (message « Déjà initialisé », aucun <c>init-error</c>).
/// Prouve aussi sur le FS que <c>post-receive</c> existe dans le home temporaire isolé.
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement.
/// Port dédié 5275, racine résolue via Piscine.slnx, répertoires temporaires nettoyés en DisposeAsync.
/// </summary>
public sealed class InitSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247), TerminalSmokeTests (5249), CoachingSmokeTests (5251),
    // CheckSmokeTests (5253), ProgressSmokeTests (5255), NavigationSmokeTests (5257), BoardSmokeTests (5259),
    // ReaderSmokeTests (5273).
    private const int Port = 5275;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        // Répertoires temporaires isolés pour cet E2E — poste vierge, NON pré-initialisé.
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-init-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");

        // On crée _tempHome mais PAS workspace ni .state : l'init doit les créer elle-même.
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
            catch { /* le processus a déjà rendu l'âme */ }
        }
        _host?.Dispose();

        // Nettoyer les répertoires temporaires.
        // git marque ses fichiers d'objets en lecture seule (surtout sous Windows) :
        // on lève l'attribut avant suppression, sinon Delete échoue.
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
    public async Task Init_on_blank_then_idempotent()
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
            await page.GotoAsync($"{BaseUrl}/init", new PageGotoOptions { Timeout = 30_000 });

            // Attendre que le composant interactif soit rendu (SignalR circuit établi).
            await page.WaitForSelectorAsync(
                "[data-testid='init-status']",
                new PageWaitForSelectorOptions { Timeout = 30_000 });

            // Poste vierge → l'overlay onboarding (1ᵉʳ lancement) recouvre la page et intercepterait le
            // clic « Initialiser ». On le ferme (Dismiss n'initialise rien : data-initialized reste False).
            await OnboardingOverlay.DismissIfPresentAsync(page);

            // ASSERTION 1 : poste vierge → data-initialized="False".
            var statusEl = await page.QuerySelectorAsync("[data-testid='init-status']")
                ?? throw new InvalidOperationException("init-status introuvable après WaitForSelector.");
            var initializedBefore = await statusEl.GetAttributeAsync("data-initialized");
            Assert.Equal("False", initializedBefore);

            // CLIC Init (boucle jusqu'à ce que le circuit Blazor Server traite le clic).
            var clickDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (DateTime.UtcNow < clickDeadline)
            {
                await page.ClickAsync("[data-testid='run-init']");
                var result = await page.QuerySelectorAsync("[data-testid='init-result']");
                if (result is not null)
                {
                    break;
                }

                await Task.Delay(500);
            }

            // Attendre init-result (GitWorkspace.Initialize crée le bare repo + hook).
            await page.WaitForSelectorAsync(
                "[data-testid='init-result']",
                new PageWaitForSelectorOptions { Timeout = 60_000 });

            // ASSERTION 2 : init-result data-success="True".
            var resultEl = await page.QuerySelectorAsync("[data-testid='init-result']")
                ?? throw new InvalidOperationException("init-result introuvable après WaitForSelector.");
            var success = await resultEl.GetAttributeAsync("data-success");
            Assert.Equal("True", success);

            // ASSERTION 3 : pas d'init-error.
            var errorEls = await page.Locator("[data-testid='init-error']").CountAsync();
            Assert.Equal(0, errorEls);

            // ASSERTION 4 : init-status passe data-initialized="True" après le clic.
            // Le composant met à jour _status = _outcome.After via StateHasChanged.
            // On ré-interroge l'élément pour avoir la valeur fraîche après le rendu async.
            await page.WaitForFunctionAsync(
                "document.querySelector('[data-testid=\"init-status\"]')?.getAttribute('data-initialized') === 'True'",
                null,
                new PageWaitForFunctionOptions { Timeout = 15_000 });

            statusEl = await page.QuerySelectorAsync("[data-testid='init-status']")
                ?? throw new InvalidOperationException("init-status disparu après init.");
            var initializedAfter = await statusEl.GetAttributeAsync("data-initialized");
            Assert.Equal("True", initializedAfter);

            // ASSERTION 5 (FS) : le hook post-receive existe bien dans le home temporaire isolé.
            var expectedHook = Path.Combine(_tempHome!, ".state", "remote.git", "hooks", "post-receive");
            Assert.True(File.Exists(expectedHook),
                $"Le hook post-receive est absent : {expectedHook}. " +
                "Vérifier que GitWorkspace.Initialize a bien écrit dans PISCINE_HOME/.state/remote.git/hooks/.");

            // ── IDEMPOTENCE ──────────────────────────────────────────────────────────────────────
            // Re-cliquer le bouton Init : doit retourner succès + message « Déjà initialisé ».
            await page.ClickAsync("[data-testid='run-init']");

            // Attendre que init-result soit rafraîchi (data-success doit rester True).
            await page.WaitForFunctionAsync(
                "document.querySelector('[data-testid=\"init-result\"]') !== null",
                null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });

            // Attendre que le message contienne « Déjà initialisé » (le moteur renvoie cette chaîne).
            await page.WaitForFunctionAsync(
                "document.querySelector('[data-testid=\"init-result\"]')?.innerText?.includes('Déjà initialisé')",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 });

            // ASSERTION 6 : re-clic → data-success="True" (idempotent, pas d'erreur).
            resultEl = await page.QuerySelectorAsync("[data-testid='init-result']")
                ?? throw new InvalidOperationException("init-result disparu après re-clic.");
            var successIdem = await resultEl.GetAttributeAsync("data-success");
            Assert.Equal("True", successIdem);

            // ASSERTION 7 : message contient « Déjà initialisé ».
            var resultText = await resultEl.InnerTextAsync();
            Assert.Contains("Déjà initialisé", resultText);

            // ASSERTION 8 : toujours aucun init-error.
            var errorElsIdem = await page.Locator("[data-testid='init-error']").CountAsync();
            Assert.Equal(0, errorElsIdem);
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

    /// <summary>Lève l'attribut lecture seule sur tous les fichiers du dossier (git les marque read-only sous Windows).</summary>
    private static void ClearReadOnly(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }
}
