using System.Diagnostics;
using System.Text.Json;
using Photino.Blazor;

namespace Piscine.Desktop;

/// <summary>
/// Sonde de rendu « smoke » (activée par <c>PISCINE_SMOKE=1</c> + <c>PISCINE_SMOKE_OUT=&lt;fichier&gt;</c>).
/// La page (voir <c>wwwroot/index.html</c>) envoie périodiquement, par <b>web message</b> Photino, un
/// bilan du DOM réellement rendu dans la webview ; cette sonde l'écrit en JSON puis termine le process
/// après un délai. Elle permet de <b>prouver que la fenêtre Photino affiche du contenu</b> (pas un écran
/// noir) — ce qu'un smoke « le process reste vivant » ne détecte pas. Inerte hors mode smoke.
/// </summary>
internal static class SmokeProbe
{
    private const string Prefix = "PISCINE_SMOKE:";

    public static void Attach(PhotinoBlazorApp app, string outPath, int timeoutSeconds = 12)
    {
        var received = 0;

        app.MainWindow.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                // Le dernier bilan reçu écrase le précédent (on capture l'état après stabilisation).
                File.WriteAllText(outPath, message[Prefix.Length..]);
                Interlocked.Exchange(ref received, 1);
            }
            catch
            {
                // best-effort : ne jamais faire planter l'hôte depuis la sonde.
            }
        });

        // Chien de garde : termine le process à l'échéance, que la page ait répondu ou non. Si aucun
        // bilan n'est arrivé, écrit un marqueur explicite (la page ne s'est jamais chargée/exécutée).
        // Le test (DesktopRenderSmokeTests) ne dépend PAS de cet auto-arrêt — il sonde le bilan JSON puis
        // tue l'arbre de process — mais on s'efforce quand même de terminer proprement (lancement manuel,
        // pas de process fantôme). Sur Windows, Environment.Exit(0) NE suffit PAS : la boucle de messages
        // native de WebView2 maintient le process vivant. On tente donc un arrêt propre, puis on force.
        _ = new Timer(
            _ =>
            {
                if (Volatile.Read(ref received) == 0)
                {
                    try
                    {
                        File.WriteAllText(
                            outPath,
                            JsonSerializer.Serialize(new { received = false, reason = "timeout" }));
                    }
                    catch
                    {
                        // best-effort
                    }
                }

                Terminate(app);
            },
            state: null,
            dueTime: TimeSpan.FromSeconds(timeoutSeconds),
            period: Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Termine le process de façon robuste sur toutes les plateformes. Programme d'abord un force-kill
    /// différé en <b>dernier recours</b> (sur un autre thread, pour ne pas dépendre du retour de
    /// <c>Environment.Exit</c>), tente une fermeture propre de la fenêtre Photino (libère la boucle de
    /// messages native), puis demande l'arrêt CLR. Sous Windows, la boucle WebView2 ignore
    /// <c>Environment.Exit(0)</c> et garderait le process vivant : le force-kill différé prend alors le
    /// relais. Tout est best-effort.
    /// </summary>
    private static void Terminate(PhotinoBlazorApp app)
    {
        // 1) Filet de sécurité programmé EN PREMIER, sur un thread de pool indépendant : si l'arrêt propre
        //    ci-dessous bloque (cas Windows/WebView2 où Environment.Exit(0) ne termine pas le process), ce
        //    timer force-kill le process courant. Inoffensif si l'arrêt propre a déjà réussi.
        _ = new Timer(
            _ =>
            {
                try { Process.GetCurrentProcess().Kill(); }
                catch { /* best-effort */ }
            },
            state: null,
            dueTime: TimeSpan.FromMilliseconds(750),
            period: Timeout.InfiniteTimeSpan);

        // 2) Fermeture propre de la fenêtre : tente de débloquer la boucle de messages native.
        try { app.MainWindow.Close(); }
        catch { /* best-effort */ }

        // 3) Demande d'arrêt CLR « propre » (flush, finalizers de fin de process).
        try { Environment.Exit(0); }
        catch { /* best-effort */ }
    }
}
