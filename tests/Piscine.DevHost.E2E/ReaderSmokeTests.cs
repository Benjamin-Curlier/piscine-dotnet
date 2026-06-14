using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E /module/{id} : démarre le DevHost avec le contenu réel, pilote Chromium vers la page
/// d'un module riche en titres (<c>05-git-intermediaire</c>) et vérifie :
/// (1) le cours est rendu (h1 présent) ;
/// (2) le sommaire <c>data-testid="course-toc"</c> est présent avec au moins un lien ancré ;
/// (3) le premier lien du sommaire pointe vers un id réellement présent dans le DOM (ancres cohérentes
///     avec les ids émis par Markdig, y compris les titres accentués) ;
/// (4) la coloration syntaxique highlight.js a tourné (au moins un <c>pre code.hljs</c> ou classe hljs-* présente) ;
/// (5) la bascule clair/sombre modifie <c>data-theme</c> sur <c>&lt;html&gt;</c> et persiste dans
///     <c>localStorage.theme</c>.
/// Si Chromium n'est pas installé (CI sans <c>playwright install</c>), le test se SAUTE proprement.
/// Port dédié 5273, racine résolue via Piscine.slnx, PISCINE_CONTENT pointé sur content/.
/// </summary>
public sealed class ReaderSmokeTests : IAsyncLifetime
{
    // Port dédié, distinct de SmokeTests (5247), TerminalSmokeTests (5249), CoachingSmokeTests (5251),
    // CheckSmokeTests (5253), ProgressSmokeTests (5255), NavigationSmokeTests (5257).
    private const int Port = 5273;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    // Module avec titres ## accentués + blocs de code bash → idéal pour les assertions sommaire + hljs.
    private const string ModuleId = "05-git-intermediaire";

    private Process? _host;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_CONTENT"] = contentDir;

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
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Reader_renders_course_with_toc_anchors_colorization_and_theme_toggle()
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
            await page.GotoAsync($"{BaseUrl}/module/{ModuleId}", new PageGotoOptions { Timeout = 30_000 });

            // ── (1) Cours rendu : le h1 du cours est présent ──────────────────────────────────────
            await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // ── (2) Sommaire présent avec au moins un lien ancré ──────────────────────────────────
            var tocLocator = page.Locator("[data-testid='course-toc']");
            var tocCount = await tocLocator.CountAsync();
            Assert.True(tocCount > 0, "Le sommaire [data-testid='course-toc'] est absent de /module/05-git-intermediaire.");

            var tocLinks = page.Locator("nav.toc a[href^='#']");
            var linkCount = await tocLinks.CountAsync();
            Assert.True(linkCount > 0, "Le sommaire ne contient aucun lien ancré (nav.toc a[href^='#']).");

            // ── (3) Ancre valide : le premier lien pointe vers un id présent dans le DOM ──────────
            var firstHref = await tocLinks.First.GetAttributeAsync("href");
            Assert.False(string.IsNullOrEmpty(firstHref), "Le premier lien du sommaire n'a pas d'attribut href.");

            // firstHref = "#slug" → slug = firstHref[1..]
            var slug = firstHref!.TrimStart('#');
            Assert.False(string.IsNullOrEmpty(slug), $"Le slug extrait de '{firstHref}' est vide.");

            // L'élément avec cet id doit exister dans le DOM.
            var anchorCount = await page.Locator($"#{slug}").CountAsync();
            Assert.True(
                anchorCount > 0,
                $"L'ancre #{slug} du sommaire n'a pas d'élément correspondant dans le DOM. " +
                "Les ids émis par Markdig (AdvancedExtensions + AutoIdentifiers GitHub) ne concordent pas " +
                "avec les hrefs générés par CourseToc.");

            // ── (4) Coloration syntaxique : au moins un pre code porte la classe hljs ─────────────
            // highlight.js ajoute `class="hljs ..."` ou, pour les blocs non reconnus, `class="hljs"`.
            // On attend un bref délai (highlight.js s'exécute au chargement de la page).
            await page.WaitForTimeoutAsync(1_500);
            var hljsCount = await page.Locator("pre code.hljs").CountAsync();
            Assert.True(
                hljsCount > 0,
                "Aucun 'pre code.hljs' trouvé : highlight.js ne semble pas avoir colorisé les blocs de code. " +
                "Vérifier que highlightAll() est appelé après le chargement (DevHost App.razor onPageReady/enhanced-load).");

            // ── (5) Bascule clair/sombre ──────────────────────────────────────────────────────────
            var initialTheme = await page.EvaluateAsync<string>(
                "() => document.documentElement.getAttribute('data-theme') ?? 'none'");
            Assert.False(
                string.IsNullOrEmpty(initialTheme) || initialTheme == "none",
                "Attribut data-theme absent sur <html> avant la bascule : __applyTheme() n'a pas été appelé.");

            // Cliquer le bouton de bascule thème.
            await page.ClickAsync("#theme-toggle");

            var newTheme = await page.EvaluateAsync<string>(
                "() => document.documentElement.getAttribute('data-theme') ?? 'none'");
            Assert.True(
                initialTheme != newTheme,
                $"data-theme n'a pas changé après clic sur #theme-toggle (reste '{initialTheme}'). " +
                "Vérifier toggleTheme() dans theme.js et le câblage du bouton dans MainLayout.razor.");

            // La valeur doit être 'light' ou 'dark' (pas un état invalide).
            Assert.True(
                newTheme is "light" or "dark",
                $"data-theme après bascule vaut '{newTheme}' (attendu 'light' ou 'dark').");
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
