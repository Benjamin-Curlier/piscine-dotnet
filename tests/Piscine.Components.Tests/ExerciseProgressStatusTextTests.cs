using Piscine.App.Progress;
using Piscine.Components.Components.Progress;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class ExerciseProgressStatusTextTests
{
    [Theory]
    [InlineData(ExerciseProgressStatus.NonCommence, "Non commencé", "non-commence")]
    [InlineData(ExerciseProgressStatus.EnCours, "En cours", "en-cours")]
    [InlineData(ExerciseProgressStatus.CommiteNonPousse, "Commité, non poussé", "commite-non-pousse")]
    [InlineData(ExerciseProgressStatus.PousseNote, "Poussé → noté", "pousse-note")]
    [InlineData(ExerciseProgressStatus.ARevoir, "À revoir", "a-revoir")]
    public void Label_and_suffix_match_each_status(ExerciseProgressStatus status, string label, string suffix)
    {
        Assert.Equal(label, ExerciseProgressStatusText.Label(status));
        Assert.Equal(suffix, ExerciseProgressStatusText.CssSuffix(status));
    }
}
