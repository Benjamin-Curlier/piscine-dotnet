using Bunit;
using Piscine.Components.Components.Board;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class GamificationPanelTests : BunitContext
{
    [Fact]
    public void RendersLevelProgressStreakAndBadges()
    {
        var summary = new GamificationSummary(
            300,
            2,
            250,
            750,
            4,
            3,
            new[] { new EarnedBadge("architect", "Architecte", "Mission architecture validée.") });

        var cut = Render<GamificationPanel>(parameters => parameters.Add(p => p.Summary, summary));

        Assert.Contains("Niveau 2", cut.Markup);
        Assert.Contains("300 XP", cut.Markup);
        Assert.Contains("4 jour(s)", cut.Markup);
        Assert.Contains("Architecte", cut.Markup);
        Assert.Equal("10%", cut.Find(".bar-fill").GetAttribute("style")?.Split(':')[1]);
    }
}
