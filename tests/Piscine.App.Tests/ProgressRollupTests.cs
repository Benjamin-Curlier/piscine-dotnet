using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class ProgressRollupTests
{
    [Fact]
    public void Empty_module_is_NonCommence() =>
        Assert.Equal(ExerciseProgressStatus.NonCommence, ProgressRollup.ForModule([]));

    [Fact]
    public void Any_ARevoir_wins_over_everything()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.ARevoir, ExerciseProgressStatus.EnCours];
        Assert.Equal(ExerciseProgressStatus.ARevoir, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void All_PousseNote_is_complete()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.PousseNote];
        Assert.Equal(ExerciseProgressStatus.PousseNote, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void Partial_progress_is_EnCours()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.NonCommence];
        Assert.Equal(ExerciseProgressStatus.EnCours, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void All_NonCommence_stays_NonCommence()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.NonCommence, ExerciseProgressStatus.NonCommence];
        Assert.Equal(ExerciseProgressStatus.NonCommence, ProgressRollup.ForModule(statuses));
    }
}
