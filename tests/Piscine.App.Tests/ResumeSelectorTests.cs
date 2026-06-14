using System.Collections.Generic;
using Piscine.App.Board;
using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class ResumeSelectorTests
{
    private static (string, string) Exo(string m, string e) => (m, e);

    [Fact]
    public void Empty_curriculum_returns_null() =>
        Assert.Null(ResumeSelector.Pick([], new Dictionary<(string, string), ExerciseProgressStatus>()));

    [Fact]
    public void All_done_returns_null()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
            [("m", "b")] = ExerciseProgressStatus.PousseNote,
        };
        Assert.Null(ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void Picks_first_NonCommence_when_nothing_in_progress()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
        };
        Assert.Equal(("m", "b"), ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void In_progress_beats_later_NonCommence()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b"), Exo("m", "c") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
            [("m", "b")] = ExerciseProgressStatus.EnCours,
        };
        Assert.Equal(("m", "b"), ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void ARevoir_has_highest_priority_even_if_later()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b"), Exo("m", "c") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.EnCours,
            [("m", "c")] = ExerciseProgressStatus.ARevoir,
        };
        Assert.Equal(("m", "c"), ResumeSelector.Pick(exos, st));
    }
}
