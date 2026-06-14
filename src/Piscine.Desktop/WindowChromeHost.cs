using System.Globalization;
using Photino.Blazor;

namespace Piscine.Desktop;

/// <summary>
/// Pilote la fenêtre Photino chromeless depuis la page (web messages "PISCINE_WIN:&lt;action&gt;").
/// Actions : minimize, togglemax, close, dragby:&lt;dx&gt;,&lt;dy&gt;. Annonce l'état agrandi à la page
/// (window.__winState) pour le style. Coexiste avec SmokeProbe (préfixes de message distincts).
/// </summary>
internal static class WindowChromeHost
{
    private const string Prefix = "PISCINE_WIN:";

    public static void Attach(PhotinoBlazorApp app)
    {
        var win = app.MainWindow;
        win.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal)) return;
            var cmd = message[Prefix.Length..];
            try
            {
                if (cmd == "minimize") { win.SetMinimized(true); }
                else if (cmd == "close") { win.Close(); }
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
                else if (cmd.StartsWith("resizeby:", StringComparison.Ordinal))
                {
                    var rest = cmd["resizeby:".Length..];
                    var c = rest.IndexOf(':');
                    if (c < 0) { return; }
                    var edge = rest[..c];
                    var d = rest[(c + 1)..].Split(',');
                    if (d.Length == 2
                        && int.TryParse(d[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
                        && int.TryParse(d[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dy))
                    {
                        if (edge.Contains('e')) { win.SetWidth(Math.Max(640, win.Width + dx)); }
                        if (edge.Contains('s')) { win.SetHeight(Math.Max(480, win.Height + dy)); }
                    }
                }
            }
            catch { /* best-effort : ne jamais faire planter l'hôte depuis le chrome. */ }
        });
    }
}
