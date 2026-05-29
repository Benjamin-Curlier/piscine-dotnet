using System.Collections.Generic;
using System.Linq;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GroupGraderTests
{
    private static ExerciseSubmission Submission(string id, string source, string expect)
    {
        var manifest = new ExerciseManifest
        {
            Id = id,
            Grading =
            {
                new GradingStep
                {
                    Type = "io",
                    Cases = { new IoCase { ExpectStdout = expect, ExpectExit = 0 } }
                }
            }
        };
        var context = new GradingContext(new Dictionary<string, string> { ["P.cs"] = source });
        return new ExerciseSubmission(manifest, context);
    }

    private static GroupGrader Grader() => new(new ExerciseGrader(new IGrader[] { new IoGrader() }));

    [Fact]
    public void GradeGroup_StopsAtFirstFailure_MarksRestNonCorrige()
    {
        var submissions = new[]
        {
            Submission("ex00", "System.Console.Write(\"ok\");", "ok"),       // Reussi
            Submission("ex01", "System.Console.Write(\"non\");", "attendu"), // ARevoir → stop
            Submission("ex02", "System.Console.Write(\"ok\");", "ok")        // NonCorrige
        };

        var results = Grader().GradeGroup(submissions).ToList();

        Assert.Equal(GraderStatus.Reussi, results[0].Status);
        Assert.Equal(GraderStatus.ARevoir, results[1].Status);
        Assert.Equal(GraderStatus.NonCorrige, results[2].Status);
        Assert.Equal("ex02", results[2].ExerciseId);
    }

    [Fact]
    public void GradeGroup_AllReussi_WhenEveryExercisePasses()
    {
        var submissions = new[]
        {
            Submission("ex00", "System.Console.Write(\"a\");", "a"),
            Submission("ex01", "System.Console.Write(\"b\");", "b")
        };

        var results = Grader().GradeGroup(submissions).ToList();

        Assert.All(results, r => Assert.Equal(GraderStatus.Reussi, r.Status));
    }
}
