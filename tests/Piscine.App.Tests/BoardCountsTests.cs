using Piscine.App.Board;
using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class BoardCountsTests
{
    [Fact]
    public void Counts_and_percent_are_correct()
    {
        ExerciseProgressStatus[] s =
        [
            ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.PousseNote,
            ExerciseProgressStatus.EnCours, ExerciseProgressStatus.CommiteNonPousse,
            ExerciseProgressStatus.ARevoir, ExerciseProgressStatus.NonCommence,
        ];
        var c = BoardCounts.From(s);
        Assert.Equal(2, c.Fait);
        Assert.Equal(2, c.EnCours);
        Assert.Equal(1, c.ARevoir);
        Assert.Equal(1, c.Restant);
        Assert.Equal(6, c.Total);
        Assert.Equal(33, c.PercentFait); // 2/6 = 33 % (arrondi)
    }

    [Fact]
    public void Empty_is_all_zero_no_divide_by_zero()
    {
        var c = BoardCounts.From([]);
        Assert.Equal(0, c.Total);
        Assert.Equal(0, c.PercentFait);
    }
}
