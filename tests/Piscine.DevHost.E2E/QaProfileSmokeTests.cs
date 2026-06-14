using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke du harnais QA agentique : pour chaque profil de seed, le DevHost démarre via
/// <c>PISCINE_QA_PROFILE</c> (pas de progress.json planté en C#) et rend son <c>data-testid</c>
/// emblématique. Cela vérifie que le seeder (DevHost/Qa) câble bien l'état attendu de bout en bout :
/// <list type="bullet">
///   <item><c>fresh</c> → overlay onboarding (workspace non initialisé) ;</item>
///   <item><c>mixed</c> → pastilles de progression (état initialisé + progression variée).</item>
/// </list>
/// Port dédié 5283 (distinct de 5247/5249/.../5281). Skip propre sans Chromium
/// (CI sans <c>playwright install</c>). Racine résolue via Piscine.slnx, temp nettoyé en fin de test.
/// </summary>
public sealed class QaProfileSmokeTests
{
    private const int Port = 5283;

    [Theory]
    [InlineData("fresh", "[data-testid='onboarding']")]   // overlay onboarding (1ᵉʳ lancement)
    [InlineData("mixed", "[data-testid='status-dot']")]   // pastilles de progression
    public async Task Profile_boots_into_expected_state(string profile, string hallmark)
    {
        var repoRoot = FindRepoRoot();
        var home = Path.Combine(Path.GetTempPath(), $"piscine-qa-{profile}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(home, "workspace"));
        var url = $"http://localhost:{Port}";

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{Path.Combine(repoRoot, "src", "Piscine.DevHost")}\" --urls {url}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_HOME"] = home;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = Path.Combine(home, "workspace");
        psi.EnvironmentVariables["PISCINE_CONTENT"] = Path.Combine(repoRoot, "content");
        psi.EnvironmentVariables["PISCINE_QA_PROFILE"] = profile;

        using var host = Process.Start(psi)
            ?? throw new InvalidOperationException("Impossible de démarrer le DevHost.");
        try
        {
            await WaitForServerAsync(url, TimeSpan.FromSeconds(90));

            using var pw = await Playwright.CreateAsync();
            IBrowser browser;
            try
            {
                browser = await pw.Chromium.LaunchAsync();
            }
            catch (PlaywrightException)
            {
                return; // Chromium absent (CI sans `playwright install`) : skip propre.
            }

            await using (browser)
            {
                var page = await browser.NewPageAsync();
                await page.GotoAsync(url, new PageGotoOptions { Timeout = 30_000 });

                // L'overlay onboarding (fresh) et les pastilles (mixed) sont des îles InteractiveServer :
                // elles apparaissent une fois le circuit SignalR monté → attente sur le sélecteur.
                await page.WaitForSelectorAsync(hallmark, new PageWaitForSelectorOptions { Timeout = 30_000 });

                Assert.True(
                    await page.Locator(hallmark).CountAsync() > 0,
                    $"Profil '{profile}' : sélecteur emblématique '{hallmark}' introuvable.");
            }
        }
        finally
        {
            try { host.Kill(entireProcessTree: true); } catch { /* déjà mort */ }
            try { ClearReadOnly(home); Directory.Delete(home, recursive: true); } catch { /* pas critique */ }
        }
    }

    private static async Task WaitForServerAsync(string url, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if ((await http.GetAsync(url)).IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) { /* pas prêt */ }
            catch (TaskCanceledException) { /* retente */ }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Le DevHost n'a pas répondu sur {url} ({timeout.TotalSeconds:0}s).");
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

    /// <summary>Dégèle les objets git en lecture seule (Windows) avant suppression du temp.</summary>
    private static void ClearReadOnly(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try { File.SetAttributes(file, FileAttributes.Normal); }
            catch { /* best-effort */ }
        }
    }
}
