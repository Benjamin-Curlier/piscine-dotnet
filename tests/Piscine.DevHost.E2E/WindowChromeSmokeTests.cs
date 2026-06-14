using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la parité navigateur du chrome de fenêtre custom : démarre le DevHost, puis vérifie
/// qu'en navigateur (1) <c>html[data-host]="browser"</c> (le module windowChrome.js détecte l'absence
/// d'hôte Photino), (2) les contrôles de fenêtre sont MASQUÉS, (3) la navigation reste intacte
/// (onglet « Cours » présent). Skip propre sans Chromium.
/// Port dédié 5281 (distinct de 5247/5249/5251/5253/5255/5257).
/// </summary>
public sealed class WindowChromeSmokeTests : IAsyncLifetime
{
    private const int Port = 5281;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-chrome-{Guid.NewGuid():N}");
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
    public async Task Browser_hides_window_controls_and_keeps_nav()
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
            await page.WaitForSelectorAsync("header.navbar", new PageWaitForSelectorOptions { Timeout = 30_000 });

            var host = await page.EvalOnSelectorAsync<string>("html", "el => el.getAttribute('data-host')");
            Assert.Equal("browser", host);

            var controls = await page.QuerySelectorAsync(".window-controls");
            if (controls is not null)
            {
                Assert.False(
                    await controls.IsVisibleAsync(),
                    "Les contrôles de fenêtre doivent être masqués en navigateur.");
            }

            Assert.NotNull(await page.QuerySelectorAsync("[data-testid='nav-cours']"));
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
