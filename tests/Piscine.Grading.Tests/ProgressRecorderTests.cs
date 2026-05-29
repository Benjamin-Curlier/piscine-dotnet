using System;
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ProgressRecorderTests
{
    [Fact]
    public void Apply_RecordsReussiAndARevoir_AndSkipsNonCorrige()
    {
        var progress = new Progress();
        var results = new[]
        {
            new ExerciseGradingResult("ex00", new[] { GraderResult.Success("io") }),
            new ExerciseGradingResult("ex01", new[] { GraderResult.Failure("io", "KO") }),
            ExerciseGradingResult.NotGraded("ex02")
        };

        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);

        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
        Assert.Equal(ExerciseStatus.ARevoir, progress.Exercises["ex01"].Status);
        Assert.False(progress.Exercises.ContainsKey("ex02"));
        Assert.Equal(1, progress.Exercises["ex00"].Attempts);
    }

    [Fact]
    public void Apply_IncrementsAttempts_OnRepeatedGrading()
    {
        var progress = new Progress();
        var results = new[] { new ExerciseGradingResult("ex00", new[] { GraderResult.Failure("io", "KO") }) };

        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);
        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, progress.Exercises["ex00"].Attempts);
    }
}
