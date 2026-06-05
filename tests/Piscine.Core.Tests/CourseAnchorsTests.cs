using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class CourseAnchorsTests
{
    [Fact]
    public void Extract_ExplicitId_Wins()
    {
        var anchors = CourseAnchors.Extract("## 2. Tri à bulles {#tri-bulle}\n");

        Assert.Contains("tri-bulle", anchors);
        Assert.DoesNotContain("2-tri-à-bulles", anchors);
    }

    [Fact]
    public void Extract_WithoutExplicitId_FallsBackToGitHubSlug()
    {
        var anchors = CourseAnchors.Extract("### Pourquoi c'est important ?\n");

        Assert.Contains("pourquoi-cest-important", anchors);
    }

    [Fact]
    public void Extract_HandlesAllAtxLevelsAndIgnoresNonHeadings()
    {
        const string md = """
            # Titre {#top}
            texte normal
            ###### Niveau 6 {#six}
            #pas-un-titre (pas d'espace)
            ####### Sept dieses {#sept}
            """;

        var anchors = CourseAnchors.Extract(md);

        Assert.Contains("top", anchors);
        Assert.Contains("six", anchors);
        Assert.DoesNotContain("sept", anchors); // 7 dièses ≠ titre ATX
        Assert.Equal(2, anchors.Count);
    }

    [Theory]
    [InlineData("cours.md#tri-bulle", "cours.md", "tri-bulle")]
    [InlineData("cours.md", "cours.md", null)]
    [InlineData("cours.md#", "cours.md", "")]
    public void ParseRef_SplitsFileAndAnchor(string input, string expectedFile, string? expectedAnchor)
    {
        var (file, anchor) = CourseAnchors.ParseRef(input);

        Assert.Equal(expectedFile, file);
        Assert.Equal(expectedAnchor, anchor);
    }

    [Fact]
    public void Extract_EmptyDocument_NoAnchors()
    {
        Assert.Empty(CourseAnchors.Extract(string.Empty));
    }
}
