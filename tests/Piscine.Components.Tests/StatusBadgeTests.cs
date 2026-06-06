using Bunit;
using Piscine.App.Progress;
using Piscine.Components.Components.Progress;
using Xunit;

namespace Piscine.Components.Tests;

/// <summary>
/// Tests bUnit du composant <see cref="StatusBadge"/> : libellé FR + <c>data-status</c> par statut,
/// et infobulle « best-effort » quand le statut est déduit de l'état git (<see cref="StatusSource.GitDerived"/>).
/// </summary>
public sealed class StatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(ExerciseProgressStatus.NonCommence, "Non commencé", "NonCommence")]
    [InlineData(ExerciseProgressStatus.EnCours, "En cours", "EnCours")]
    [InlineData(ExerciseProgressStatus.CommiteNonPousse, "Commité, non poussé", "CommiteNonPousse")]
    [InlineData(ExerciseProgressStatus.PousseNote, "Poussé → noté", "PousseNote")]
    [InlineData(ExerciseProgressStatus.ARevoir, "À revoir", "ARevoir")]
    public void Render_EachStatus_ShowsLabelAndDataStatus(
        ExerciseProgressStatus status, string expectedLabel, string expectedDataStatus)
    {
        var cut = Render<StatusBadge>(p => p.Add(c => c.Status, status));

        var badge = cut.Find("[data-testid='status-badge']");
        Assert.Contains(expectedLabel, badge.TextContent, System.StringComparison.Ordinal);
        Assert.Equal(expectedDataStatus, badge.GetAttribute("data-status"));
    }

    [Fact]
    public void Render_GitDerivedSource_AddsBestEffortTitle()
    {
        var cut = Render<StatusBadge>(p => p
            .Add(c => c.Status, ExerciseProgressStatus.PousseNote)
            .Add(c => c.Source, StatusSource.GitDerived));

        var title = cut.Find("[data-testid='status-badge']").GetAttribute("title") ?? string.Empty;
        Assert.Contains("best-effort", title, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ProgressSource_HasNoBestEffortTitle()
    {
        var cut = Render<StatusBadge>(p => p
            .Add(c => c.Status, ExerciseProgressStatus.ARevoir)
            .Add(c => c.Source, StatusSource.Progress));

        var title = cut.Find("[data-testid='status-badge']").GetAttribute("title") ?? string.Empty;
        Assert.DoesNotContain("best-effort", title, System.StringComparison.Ordinal);
    }
}
