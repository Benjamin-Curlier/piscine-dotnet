using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Piscine.Components;

/// <summary>
/// Indirection des render modes pour partager les pages de cette RCL entre deux hôtes :
/// <list type="bullet">
///   <item><b>Piscine.DevHost</b> (Blazor Web App, SignalR) : les propriétés valent
///   <see cref="RenderMode.InteractiveServer"/> &amp; co. → les pages avec
///   <c>@rendermode InteractiveServer</c> sont interactives côté serveur.</item>
///   <item><b>Piscine.Desktop</b> (Photino / BlazorWebView, in-process) : le render mode n'existe pas
///   (les composants sont interactifs par nature). L'hôte appelle
///   <see cref="ConfigureBlazorHybridRenderModes"/> au démarrage pour annuler les propriétés
///   → <c>@rendermode null</c> = aucun render mode = rendu in-process.</item>
/// </list>
/// Les pages RCL écrivent <c>@rendermode InteractiveServer</c> ; via
/// <c>@using static Piscine.Components.InteractiveRenderSettings</c> dans <c>_Imports.razor</c>, ce
/// symbole résout vers <see cref="InteractiveServer"/> (et non vers le membre du framework), donc
/// l'hôte WebView peut le neutraliser. Motif documenté par Microsoft (Blazor Hybrid + RCL partagée).
/// </summary>
public static class InteractiveRenderSettings
{
    /// <summary>Render mode serveur interactif (Web App) ; <c>null</c> en hôte WebView.</summary>
    public static IComponentRenderMode? InteractiveServer { get; set; } = RenderMode.InteractiveServer;

    /// <summary>Render mode auto interactif (Web App) ; <c>null</c> en hôte WebView.</summary>
    public static IComponentRenderMode? InteractiveAuto { get; set; } = RenderMode.InteractiveAuto;

    /// <summary>Render mode WebAssembly interactif (Web App) ; <c>null</c> en hôte WebView.</summary>
    public static IComponentRenderMode? InteractiveWebAssembly { get; set; } = RenderMode.InteractiveWebAssembly;

    /// <summary>
    /// Annule tous les render modes : à appeler au démarrage d'un hôte <c>BlazorWebView</c> (Photino),
    /// où les composants sont interactifs par défaut et où les render modes ne s'appliquent pas.
    /// </summary>
    public static void ConfigureBlazorHybridRenderModes()
    {
        InteractiveServer = null;
        InteractiveAuto = null;
        InteractiveWebAssembly = null;
    }
}
