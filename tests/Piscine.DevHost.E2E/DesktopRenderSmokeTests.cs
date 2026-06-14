using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke de <b>rendu</b> de l'app de bureau Photino (<c>Piscine.Desktop</c>). Lance l'app en mode
/// sonde (<c>PISCINE_SMOKE=1</c>) : la page renvoie, par web message Photino, un bilan du DOM
/// <b>réellement rendu</b> dans la webview (voir <c>src/Piscine.Desktop/SmokeProbe.cs</c> +
/// <c>wwwroot/index.html</c>) ; <c>SmokeProbe</c> l'écrit en JSON. On asserte que la fenêtre affiche
/// du contenu — ce qu'un smoke « le process reste vivant » ne détecte PAS (cf. écran noir constaté le
/// 2026-06-14 : <c>docs/superpowers/retex/2026-06-14-desktop-blank-screen.md</c>).
///
/// <b>Succès = bilan JSON, pas auto-arrêt du process.</b> Sur Windows, la webview (WebView2) maintient
/// vivante la boucle de messages native : <c>Environment.Exit(0)</c> du chien de garde ne tue PAS le
/// process et <c>WaitForExit</c> expire alors même que le rendu a réussi (faux négatif). On <b>sonde
/// donc le fichier de bilan</b> jusqu'à <c>received==true</c> &amp;&amp; <c>appTextLen&gt;0</c>, puis on
/// <b>tue l'arbre de process</b> nous-mêmes — sans jamais dépendre de l'auto-arrêt de l'app.
///
/// <b>Opt-in</b> : ne s'exécute que si <c>PISCINE_DESKTOP_SMOKE=1</c> (la fenêtre exige un affichage —
/// WebView2 / webkitgtk + xvfb). Sans le drapeau, retour anticipé = skip, pour garder la run CI verte.
/// </summary>
public sealed class DesktopRenderSmokeTests
{
    // Marge généreuse : couvre le `dotnet run` (restore + build Release) puis l'ouverture de la fenêtre
    // et la stabilisation du DOM avant que la sonde n'émette un bilan exploitable.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

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

        var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Impossible de lancer Piscine.Desktop.");

        try
        {
            // Succès basé sur le BILAN écrit par la sonde — pas sur l'auto-arrêt du process (cf. faux
            // négatif Windows ci-dessus). On sonde le fichier jusqu'à un rendu prouvé, ou échéance.
            var deadline = DateTime.UtcNow + PollTimeout;
            string? lastJson = null;

            while (DateTime.UtcNow < deadline)
            {
                if (TryReadSuccessfulReport(outPath, out lastJson))
                {
                    return; // Rendu prouvé : la fenêtre affiche du contenu. Nettoyage dans le finally.
                }

                if (proc.HasExited && proc.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Piscine.Desktop s'est terminé (code {proc.ExitCode}) sans bilan de rendu exploitable. "
                        + $"Dernier bilan : {lastJson ?? "(aucun)"}");
                }

                Thread.Sleep(PollInterval);
            }

            // Échéance dépassée sans rendu prouvé → vrai échec (écran noir / Blazor n'a pas rendu).
            throw new TimeoutException(
                "La fenêtre Photino n'a pas prouvé de rendu dans le délai imparti "
                + $"(received==true && appTextLen>0). Dernier bilan : {lastJson ?? "(aucun bilan écrit)"}");
        }
        finally
        {
            // On NE dépend PAS de l'auto-arrêt de l'app : on tue l'arbre de process (Photino + WebView2 /
            // msedgewebview2 enfants) pour ne laisser aucun process résiduel.
            try { proc.Kill(entireProcessTree: true); } catch { /* déjà mort */ }
            try { proc.WaitForExit(10_000); } catch { /* best-effort */ }
            proc.Dispose();
            try { File.Delete(outPath); } catch { /* best-effort */ }
        }
    }

    /// <summary>
    /// Lit le fichier de bilan et indique si la sonde a prouvé un rendu (<c>received==true</c> et
    /// <c>appTextLen&gt;0</c>). Tolérant : un fichier absent, vide, partiellement écrit ou un marqueur
    /// d'échec (<c>received==false</c>) renvoient <c>false</c> ; le dernier contenu lu est exposé pour
    /// le diagnostic d'échec.
    /// </summary>
    private static bool TryReadSuccessfulReport(string outPath, out string? lastJson)
    {
        lastJson = null;

        if (!File.Exists(outPath))
        {
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(outPath);
        }
        catch (IOException)
        {
            // Écriture concurrente de la sonde : on réessaiera au prochain tour de boucle.
            return false;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        lastJson = json;

        try
        {
            using var report = JsonDocument.Parse(json);
            var root = report.RootElement;

            var received = root.TryGetProperty("received", out var r) && r.ValueKind == JsonValueKind.True;
            if (!received)
            {
                return false;
            }

            var appLen = root.TryGetProperty("appTextLen", out var a)
                && a.ValueKind == JsonValueKind.Number
                ? a.GetInt32()
                : 0;

            return appLen > 0;
        }
        catch (JsonException)
        {
            // Fichier en cours d'écriture (JSON tronqué) : on réessaiera.
            return false;
        }
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
