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

                Environment.Exit(0);
            },
            state: null,
            dueTime: TimeSpan.FromSeconds(timeoutSeconds),
            period: Timeout.InfiniteTimeSpan);
    }
}
