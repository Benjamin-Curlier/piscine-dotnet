using System.Globalization;
using Photino.Blazor;

namespace Piscine.Desktop;

/// <summary>
/// Pilote la fenêtre Photino chromeless depuis la page (web messages "PISCINE_WIN:&lt;action&gt;").
/// Actions : minimize, close, querystate, maximizeonstart, togglemax, dragby, resizestart/resizeto.
/// Annonce l'état agrandi à la page (window.__winState) pour le style. Coexiste avec SmokeProbe.
/// </summary>
/// <remarks>
/// Le PLEIN ÉCRAN au lancement est déclenché par la page (maximizeonstart), donc après l'affichage de la
/// fenêtre : c'est l'OS qui maximise (fiable et correct en DPI). Le resize par poignées applique le delta
/// TOTAL depuis une ancre figée au pointerdown (et non des deltas incrémentaux sur win.Width, qui
/// dérivaient/laggaient derrière le curseur).
/// </remarks>
internal static class WindowChromeHost
{
    private const string Prefix = "PISCINE_WIN:";

    public static void Attach(PhotinoBlazorApp app)
    {
        var win = app.MainWindow;

        var anchorW = 0;
        var anchorH = 0;

        win.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return;
            }

            var cmd = message[Prefix.Length..];
            try
            {
                if (cmd == "minimize") { win.SetMinimized(true); }
                else if (cmd == "close") { win.Close(); }
                else if (cmd == "querystate")
                {
                    win.SendWebMessage("PISCINE_WIN_STATE:" + (win.Maximized ? "maximized" : "normal"));
                }
                else if (cmd == "maximizeonstart")
                {
                    // Plein écran au lancement, déclenché après l'affichage : l'OS maximise (DPI correct).
                    if (!win.Maximized)
                    {
                        win.SetMaximized(true);
                        win.SendWebMessage("PISCINE_WIN_STATE:maximized");
                    }
                }
                else if (cmd == "togglemax")
                {
                    var max = !win.Maximized;
                    win.SetMaximized(max);
                    win.SendWebMessage("PISCINE_WIN_STATE:" + (max ? "maximized" : "normal"));
                }
                else if (cmd.StartsWith("dragby:", StringComparison.Ordinal))
                {
                    var parts = cmd["dragby:".Length..].Split(',');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dy))
                    {
                        if (win.Maximized) { win.SetMaximized(false); win.SendWebMessage("PISCINE_WIN_STATE:normal"); }
                        win.SetLeft(win.Left + dx);
                        win.SetTop(win.Top + dy);
                    }
                }
                else if (cmd.StartsWith("resizestart:", StringComparison.Ordinal))
                {
                    if (win.Maximized) { win.SetMaximized(false); win.SendWebMessage("PISCINE_WIN_STATE:normal"); }
                    anchorW = System.Math.Max(640, win.Width);
                    anchorH = System.Math.Max(480, win.Height);
                }
                else if (cmd.StartsWith("resizeto:", StringComparison.Ordinal))
                {
                    var rest = cmd["resizeto:".Length..];
                    var c = rest.IndexOf(':');
                    if (c < 0) { return; }
                    var edge = rest[..c];
                    var d = rest[(c + 1)..].Split(',');
                    if (d.Length == 2
                        && int.TryParse(d[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
                        && int.TryParse(d[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dy))
                    {
                        // Delta TOTAL depuis l'ancre : la fenêtre suit exactement le curseur, sans dérive.
                        if (edge.Contains('e')) { win.SetWidth(System.Math.Max(640, anchorW + dx)); }
                        if (edge.Contains('s')) { win.SetHeight(System.Math.Max(480, anchorH + dy)); }
                    }
                }
            }
            catch { /* best-effort : ne jamais faire planter l'hôte depuis le chrome. */ }
        });
    }
}
