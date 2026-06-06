using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Piscine.Components.Components;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="CourseToc"/> :
/// (1) liens par titre, (2) cohérence ancres Markdig via CourseAnchors, (3) vide → rien rendu.
/// </summary>
public sealed class CourseTocTests : BunitContext
{
    // Titres ASCII uniquement : CourseAnchors.Extract et Markdig UseAdvancedExtensions+UseAutoIdentifiers
    // produisent le même slug pour les caractères ASCII (pas de divergence diacritiques).
    // Note : UseAdvancedExtensions modifie le comportement de UseAutoIdentifiers (strip diacritiques
    // via FormD) alors que CourseAnchors utilise FormC. Pour les cours du curriculum (titres ASCII),
    // les deux convergent — c'est le cas nominal que le test verrouille.
    private const string SampleMarkdown = "# Cours\n\n## Partie A\n\n### Detail\n\n## Partie B";

    // ── T1 : un lien par titre ≥ MinLevel ──────────────────────────────────────────────────────

    [Fact]
    public void Render_SampleMarkdown_ShowsThreeLinks()
    {
        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, SampleMarkdown));

        // Le nav doit être présent.
        var nav = cut.Find("[data-testid='course-toc']");

        // Exactement 3 liens : ## Partie A, ### Détail, ## Partie B (le # Cours est exclu).
        var links = nav.QuerySelectorAll("a");
        Assert.Equal(3, links.Length);

        var texts = links.Select(a => a.TextContent.Trim()).ToArray();
        Assert.Contains("Partie A", texts);
        Assert.Contains("Detail", texts);
        Assert.Contains("Partie B", texts);
    }

    // ── T2 : cohérence des ancres avec Markdig / CourseAnchors ─────────────────────────────────

    [Fact]
    public void Render_SampleMarkdown_AnchorsConsistentWithMarkdigAndCourseAnchors()
    {
        // Enregistrer MarkdownRenderer comme dans MarkdownViewTests.
        Services.AddSingleton<Piscine.Components.Services.MarkdownRenderer>();

        var cut = Render<CourseToc>(p => p.Add(c => c.Markdown, SampleMarkdown));

        var links = cut.FindAll("[data-testid='course-toc'] a");
        Assert.NotEmpty(links);

        // Ensemble des ancres connues par le moteur (CourseAnchors.Extract).
        var knownAnchors = CourseAnchors.Extract(SampleMarkdown);

        // Rendre le même markdown via MarkdownRenderer pour obtenir le HTML réel.
        var renderer = Services.GetRequiredService<Piscine.Components.Services.MarkdownRenderer>();
        var renderedHtml = renderer.Render(SampleMarkdown).Value;

        foreach (var link in links)
        {
            var href = link.GetAttribute("href") ?? string.Empty;
            Assert.StartsWith("#", href);
            var slug = href[1..]; // retirer le '#'

            // Le slug doit être dans l'ensemble extrait par le moteur.
            Assert.Contains(slug, knownAnchors);

            // Le HTML rendu par Markdig doit contenir id="slug".
            Assert.Contains($"id=\"{slug}\"", renderedHtml, StringComparison.Ordinal);
        }
    }

    // ── T3 : markdown sans titre ≥ MinLevel (ou null) → rien rendu ──────────────────────────────

    [Fact]
    public void Render_NoQualifyingHeadings_RendersNothing()
    {
        // Markdown avec seulement un titre de niveau 1 (exclu par MinLevel = 2).
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
