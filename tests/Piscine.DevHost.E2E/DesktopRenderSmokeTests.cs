using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke de <b>rendu</b> de l'app de bureau Photino (<c>Piscine.Desktop</c>). Lance l'app en mode
/// sonde (<c>PISCINE_SMOKE=1</c>) : la page renvoie, par web message Photino, un bilan du DOM
/// <b>réellement rendu</b> dans la webview (voir <c>src/Piscine.Desktop/SmokeProbe.cs</c> +
/// <c>wwwroot/index.html</c>) ; <c>SmokeProbe</c> l'écrit en JSON puis termine le process. On asserte
/// que la fenêtre affiche du contenu — ce qu'un smoke « le process reste vivant » ne détecte PAS
/// (cf. écran noir constaté le 2026-06-14 : <c>docs/superpowers/retex/2026-06-14-desktop-blank-screen.md</c>).
///
/// <b>Opt-in</b> : ne s'exécute que si <c>PISCINE_DESKTOP_SMOKE=1</c> (la fenêtre exige un affichage —
/// WebView2 / webkitgtk + xvfb). Sans le drapeau, retour anticipé = skip, pour garder la run CI verte.
/// </summary>
public sealed class DesktopRenderSmokeTests
{
    [Fact]
    public void Desktop_window_renders_content()
    {
        if (Environment.GetEnvironmentVariable("PISCINE_DESKTOP_SMOKE") != "1")
        {
            // Skip : requiert un affichage. xUnit 2.x n'a pas d'Assert.Skip → retour anticipé.
            return;
        }

        var repoRoot = FindRepoRoot();
        var outPath = Path.Combine(Path.GetTempPath(), $"piscine-desktop-smoke-{Guid.NewGuid():N}.json");

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{Path.Combine(repoRoot, "src", "Piscine.Desktop")}\" -c Release",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_SMOKE"] = "1";
        psi.EnvironmentVariables["PISCINE_SMOKE_OUT"] = outPath;
        psi.EnvironmentVariables["PISCINE_CONTENT"] = Path.Combine(repoRoot, "content");

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Impossible de lancer Piscine.Desktop.");

        // La sonde écrit le bilan puis se termine seule (chien de garde ~12 s) ; large marge pour le build.
        if (!proc.WaitForExit(milliseconds: 180_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* déjà mort */ }
            throw new TimeoutException("Piscine.Desktop (mode sonde) ne s'est pas terminé à temps.");
        }

        Assert.True(File.Exists(outPath), $"Aucun bilan de rendu écrit ({outPath}).");
        var json = File.ReadAllText(outPath);
        try { File.Delete(outPath); } catch { /* best-effort */ }

        using var report = JsonDocument.Parse(json);
        var root = report.RootElement;

        var received = root.TryGetProperty("received", out var r) && r.ValueKind == JsonValueKind.True;
        Assert.True(
            received,
            $"La fenêtre Photino n'a renvoyé AUCUN bilan : la page ne se charge pas (écran noir). Bilan : {json}");

        var appLen = root.TryGetProperty("appTextLen", out var a) ? a.GetInt32() : 0;
        Assert.True(
            appLen > 0,
            $"La page s'est chargée mais #app est vide (Blazor n'a pas rendu). Bilan : {json}");
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
