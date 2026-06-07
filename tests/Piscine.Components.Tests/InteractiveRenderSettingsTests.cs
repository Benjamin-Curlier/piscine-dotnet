using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Verrouille l'indirection des render modes (motif Blazor Hybrid + RCL partagée, cf.
/// <see cref="InteractiveRenderSettings"/>) : les pages RCL portent <c>@rendermode InteractiveServer</c>
/// (valeurs framework pour Piscine.DevHost / Blazor Web App), et un hôte <c>BlazorWebView</c> (Photino)
/// appelle <see cref="InteractiveRenderSettings.ConfigureBlazorHybridRenderModes"/> pour les annuler
/// → <c>@rendermode null</c> = rendu in-process.
/// </summary>
public sealed class InteractiveRenderSettingsTests
{
    [Fact]
    public void ConfigureBlazorHybridRenderModes_NullsRenderModes_ForWebViewHost()
    {
        // Sauvegarde l'état statique global pour ne pas fuir vers d'autres tests.
        var (server, auto, wasm) = (
            InteractiveRenderSettings.InteractiveServer,
            InteractiveRenderSettings.InteractiveAuto,
            InteractiveRenderSettings.InteractiveWebAssembly);
        try
        {
            // Valeurs « Web App » : render modes du framework (non nuls).
            InteractiveRenderSettings.InteractiveServer = RenderMode.InteractiveServer;
            InteractiveRenderSettings.InteractiveAuto = RenderMode.InteractiveAuto;
            InteractiveRenderSettings.InteractiveWebAssembly = RenderMode.InteractiveWebAssembly;
            Assert.NotNull(InteractiveRenderSettings.InteractiveServer);

            // Hôte WebView (Photino) : tout passe à null.
            InteractiveRenderSettings.ConfigureBlazorHybridRenderModes();

            Assert.Null(InteractiveRenderSettings.InteractiveServer);
            Assert.Null(InteractiveRenderSettings.InteractiveAuto);
            Assert.Null(InteractiveRenderSettings.InteractiveWebAssembly);
        }
        finally
        {
            (InteractiveRenderSettings.InteractiveServer,
             InteractiveRenderSettings.InteractiveAuto,
             InteractiveRenderSettings.InteractiveWebAssembly) = (server, auto, wasm);
        }
    }
}
