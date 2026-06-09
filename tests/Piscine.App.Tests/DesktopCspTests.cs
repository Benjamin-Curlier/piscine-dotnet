using System.IO;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>
/// #58 — la CSP de défense-en-profondeur du shell Desktop doit autoriser exactement ce que la page
/// charge (sinon le rendu casse silencieusement) : bootstrap inline + BlazorWebView ('unsafe-inline')
/// et cdnjs (highlight.js, script + thème CSS). Ce test verrouille ces autorisations contre un
/// durcissement accidentel.
/// </summary>
public sealed class DesktopCspTests
{
    private static string ReadDesktopIndexHtml()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "src", "Piscine.Desktop", "wwwroot", "index.html"));
    }

    [Fact]
    public void IndexHtml_DeclaresCsp_AllowingInlineAndCdnjs_WithHardening()
    {
        var html = ReadDesktopIndexHtml();

        Assert.Contains("http-equiv=\"Content-Security-Policy\"", html);
        // Inline (thème + highlightAll + BlazorWebView) et cdnjs (highlight.js) autorisés.
        Assert.Contains("script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com", html);
        Assert.Contains("style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com", html);
        // Durcissement sans coût pour cette page : aucun plugin, base figée.
        Assert.Contains("object-src 'none'", html);
        Assert.Contains("base-uri 'self'", html);
    }
}
