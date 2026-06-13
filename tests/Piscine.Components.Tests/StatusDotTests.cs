using Bunit;
using Piscine.App.Progress;
using Piscine.Components.Components.Progress;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class StatusDotTests : BunitContext
{
    [Theory]
    [InlineData(ExerciseProgressStatus.NonCommence, "non-commence", "Non commencé")]
    [InlineData(ExerciseProgressStatus.EnCours, "en-cours", "En cours")]
    [InlineData(ExerciseProgressStatus.CommiteNonPousse, "commite-non-pousse", "Commité, non poussé")]
    [InlineData(ExerciseProgressStatus.PousseNote, "pousse-note", "Poussé → noté")]
    [InlineData(ExerciseProgressStatus.ARevoir, "a-revoir", "À revoir")]
    public void Render_status_sets_class_data_and_aria(
        ExerciseProgressStatus status, string suffix, string label)
    {
        var cut = Render<StatusDot>(p => p.Add(c => c.Status, status));

        var dot = cut.Find("[data-testid='status-dot']");
        Assert.Contains($"status-{suffix}", dot.GetAttribute("class"), System.StringComparison.Ordinal);
        Assert.Equal(status.ToString(), dot.GetAttribute("data-status"));
        Assert.Equal(label, dot.GetAttribute("aria-label"));
        Assert.Equal(label, dot.GetAttribute("title"));
    }
}
