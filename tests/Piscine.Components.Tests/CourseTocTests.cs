using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.Components.Components;
using Piscine.Components.Services;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="CourseToc"/> : (1) un lien par titre ≥ MinLevel ;
/// (2) **ancres == ids réellement rendus par Markdig**, y compris pour des titres ACCENTÉS (cas du
/// curriculum FR) où Markdig retire les diacritiques — c'est la régression que le sommaire dérivé du
/// HTML rendu corrige ; (3) `{#id}` explicite ; (4) vide/null → rien rendu.
/// </summary>
public sealed class CourseTocTests : BunitContext
{
    public CourseTocTests()
    {
        // CourseToc rend via MarkdownRenderer (mêmes ancres que MarkdownView) → service requis en DI.
        Services.AddSingleton<MarkdownRenderer>();
    }

    [Fact]
    public void Render_Headings_ShowsOneLinkPerHeadingAtOrAboveMinLevel()
    {
        const string md = "# Cours\n\n## Partie A\n\n### Détail\n\n## Partie B";

        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, md));

        var links = cut.FindAll("[data-testid='course-toc'] a");
        Assert.Equal(3, links.Count); // les ## / ### ; le # Cours est exclu
        var texts = links.Select(a => a.TextContent.Trim()).ToArray();
        Assert.Contains("Partie A", texts);
        Assert.Contains("Détail", texts); // l'accent est CONSERVÉ à l'affichage
        Assert.Contains("Partie B", texts);
    }

    [Fact]
    public void Render_AccentedHeadings_AnchorsMatchRenderedMarkdigIds()
    {
        // Titres FR sans {#id} : Markdig (UseAdvancedExtensions) retire les diacritiques.
        const string md = "# Cours\n\n## Opérateurs\n\n## Résoudre un conflit";

        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, md));
        var renderedHtml = Services.GetRequiredService<MarkdownRenderer>().Render(md).Value;

        var links = cut.FindAll("[data-testid='course-toc'] a");
        Assert.Equal(2, links.Count);

        foreach (var link in links)
        {
            var href = link.GetAttribute("href") ?? string.Empty;
            Assert.StartsWith("#", href, StringComparison.Ordinal);
            var slug = href[1..];

            // Garde-fou central : l'ancre du sommaire DOIT correspondre à un id réellement rendu
            // (sinon le lien est cassé). C'est ce qui échouerait avec un slug re-calculé en FormC.
            Assert.Contains($"id=\"{slug}\"", renderedHtml, StringComparison.Ordinal);
        }

        // Et les ancres sont bien diacritique-strippées (comportement Markdig), pas en FormC.
        var hrefs = links.Select(a => a.GetAttribute("href")).ToArray();
        Assert.Contains("#operateurs", hrefs);
        Assert.Contains("#resoudre-un-conflit", hrefs);
    }

    [Fact]
    public void Render_ExplicitId_UsesItAndStripsFromText()
    {
        const string md = "# Cours\n\n## Historique des choses {#histo}";

        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, md));

        var link = cut.Find("[data-testid='course-toc'] a");
        Assert.Equal("#histo", link.GetAttribute("href"));
        Assert.Equal("Historique des choses", link.TextContent.Trim()); // le {#id} n'est pas affiché
    }

    [Fact]
    public void Render_NoQualifyingHeadings_RendersNothing()
    {
        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, "# Titre seul\n\nDu texte."));
        Assert.Empty(cut.FindAll("[data-testid='course-toc']"));
    }

    [Fact]
    public void Render_NullMarkdown_RendersNothing()
    {
        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, (string?)null));
        Assert.Empty(cut.FindAll("[data-testid='course-toc']"));
    }
}
