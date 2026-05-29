using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ExerciseGraderTests
{
    private static ExerciseManifest Manifest()
    {
        return new ExerciseManifest
        {
            Id = "ex00-hello",
            Grading =
            {
                new GradingStep
                {
                    Type = "io",
                    Cases = { new IoCase { ExpectStdout = "Hello, Piscine!", ExpectExit = 0 } }
                },
                new GradingStep { Type = "norme", Blocking = false }
            }
        };
    }

    private static ExerciseGrader Grader() => new(new IGrader[] { new IoGrader(), new NormeGrader() });

    [Fact]
    public void Grade_Reussi_OnCorrectSubmission()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = "System.Console.Write(\"Hello, Piscine!\");"
        };

        var result = Grader().Grade(Manifest(), new GradingContext(sources));

        Assert.Equal("ex00-hello", result.ExerciseId);
        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public void Grade_ARevoir_OnWrongOutput()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = "System.Console.Write(\"Nope\");"
        };

        var result = Grader().Grade(Manifest(), new GradingContext(sources));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
    }

    [Fact]
    public void Grade_SkipsUnknownGraderTypes()
    {
        var manifest = new ExerciseManifest
        {
            Id = "ex01",
            Grading = { new GradingStep { Type = "unit", TestFiles = { "grader/Tests.cs" } } }
        };

        var result = Grader().Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        // 'unit' n'est pas encore enregistré (It.3) : aucune étape corrigée → Reussi par défaut.
        Assert.Empty(result.Results);
        Assert.Equal(GraderStatus.Reussi, result.Status);
    }
}
