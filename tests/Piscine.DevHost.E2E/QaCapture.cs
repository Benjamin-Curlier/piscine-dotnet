using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Utilitaire de capture pour la passe QA agentique (skill qa-and-refine). Pilote un DevHost DÉJÀ lancé
/// (via scripts/devhost-qa) et capture la matrice route × thème × largeur en PNG, plus les erreurs
/// console, dans un dossier de sortie. Opt-in : ne s'exécute que si PISCINE_QA_SHOTS=1 (sinon retour
/// anticipé = skip, pour garder la CI verte). Réutilise le Chromium bundlé de Playwright (pas de Chrome
/// système requis). Variables : PISCINE_QA_URL (défaut http://localhost:5240), PISCINE_QA_OUT (dossier),
/// PISCINE_QA_TAG (préfixe de nom, ex. le profil).
/// </summary>
public sealed class QaCapture
{
    private static readonly string[] Routes =
    {
        "/", "/cours", "/rapport", "/reglages", "/check", "/terminal", "/init",
    };

    private static readonly (string Theme, int Width)[] Variants =
    {
        ("light", 1280), ("dark", 1280), ("light", 420), ("dark", 420),
    };

    [Fact]
    public async Task Capture_matrix()
    {
        if (Environment.GetEnvironmentVariable("PISCINE_QA_SHOTS") != "1")
        {
            return; // skip hors passe QA
        }

        var baseUrl = (Environment.GetEnvironmentVariable("PISCINE_QA_URL") ?? "http://localhost:5240").TrimEnd('/');
        var outDir = Environment.GetEnvironmentVariable("PISCINE_QA_OUT")
            ?? Path.Combine(Path.GetTempPath(), "piscine-qa-shots");
        var tag = Environment.GetEnvironmentVariable("PISCINE_QA_TAG") ?? "mixed";
        // Force le mode chromeless (data-host=photino) dans le navigateur pour vérifier le CSS propre à
        // Photino (chrome de fenêtre + défilement du shell) sans la fenêtre native.
        var forcePhotino = Environment.GetEnvironmentVariable("PISCINE_QA_FORCE_PHOTINO") == "1";
        Directory.CreateDirectory(outDir);

        using var pw = await Playwright.CreateAsync();
        IBrowser browser;
        try { browser = await pw.Chromium.LaunchAsync(); }
        catch (PlaywrightException) { return; } // Chromium absent : skip propre
        await using (browser)
        {
            var log = new List<string>();
            foreach (var route in Routes)
            {
                foreach (var (theme, width) in Variants)
                {
                    var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = 900 } });
                    var errors = new List<string>();
                    page.Console += (_, m) => { if (m.Type == "error") errors.Add(m.Text); };
                    page.PageError += (_, e) => errors.Add(e);

                    // Fixer le thème AVANT le rendu Blazor : poser localStorage puis charger la route.
                    await page.GotoAsync(baseUrl + "/", new() { Timeout = 30_000 });
                    await page.EvaluateAsync("t => localStorage.setItem('theme', t)", theme);
                    await page.GotoAsync(baseUrl + route, new() { Timeout = 30_000, WaitUntil = WaitUntilState.NetworkIdle });
                    await page.WaitForTimeoutAsync(400); // laisser l'îlot interactif se stabiliser

                    var scroll = "";
                    if (forcePhotino)
                    {
                        await page.EvaluateAsync("document.documentElement.setAttribute('data-host','photino')");
                        await page.WaitForTimeoutAsync(250);
                        // Confirme que .main est bien devenu le conteneur de défilement (sinon clipping).
                        scroll = " " + await page.EvaluateAsync<string>(
                            "() => { const m=document.querySelector('.main'); if(!m) return 'no-main';" +
                            " const scrollable = m.scrollHeight > m.clientHeight + 1;" +
                            " return (scrollable?'main-scrollable':'main-fits')+` sh=${m.scrollHeight} ch=${m.clientHeight}`; }");
                    }

                    var slug = route == "/" ? "root" : route.Trim('/').Replace('/', '-');
                    var name = $"{tag}-{slug}-{theme}-{width}";
                    await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, name + ".png"), FullPage = !forcePhotino });
                    log.Add($"{name}: {(errors.Count == 0 ? "no-console-errors" : "ERRORS=" + string.Join(" | ", errors))}{scroll}");
                    await page.CloseAsync();
                }
            }

            await File.WriteAllLinesAsync(Path.Combine(outDir, $"{tag}-console.txt"), log);
        }
    }
}
