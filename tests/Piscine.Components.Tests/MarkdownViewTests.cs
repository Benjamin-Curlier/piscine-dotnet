using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Piscine.Components.Tests;

public class MarkdownViewTests : BunitContext
{
    [Fact]
    public void Renders_markdown_heading_as_h1()
    {
        // Arrange : MarkdownView injecte MarkdownRenderer via DI -> l'enregistrer.
        Services.AddSingleton<Piscine.Components.Services.MarkdownRenderer>();

        // Act : rendre le composant avec un titre markdown de niveau 1.
        var cut = Render<Piscine.Components.MarkdownView>(p => p
            .Add(c => c.Markdown, "# Titre"));

        // Assert : le markdown est rendu en <h1> contenant le texte.
        cut.Find("h1");                       // lève si aucun <h1> n'est présent
        Assert.Contains("Titre", cut.Markup);
    }
}
