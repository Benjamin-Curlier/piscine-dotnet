using System.Collections.Generic;
using System.Linq;
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
    public void Grade_FailsClosed_OnUnknownGraderType()
    {
        var manifest = new ExerciseManifest
        {
            Id = "ex01",
            Grading = { new GradingStep { Type = "type-inexistant" } }
        };

        var result = Grader().Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        // Fail-closed : un type inconnu produit un échec « contenu » explicite (jamais un faux Reussi).
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        var failure = Assert.Single(result.Results);
        Assert.Equal(GraderStatus.ARevoir, failure.Status);
        Assert.Contains(failure.Messages, m => m.Contains("type de notation inconnu"));
    }

    [Fact]
    public void Grade_FailsClosed_OnEmptyGrading()
    {
        var manifest = new ExerciseManifest { Id = "ex02" };

        var result = Grader().Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        // Une étape de notation absente ne doit pas non plus « réussir » par défaut.
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Results, r => r.Messages.Any(m => m.Contains("aucune étape de notation")));
    }

    [Fact]
    public void Grade_FailsClosed_OnUnexpectedGraderException()
    {
        var manifest = new ExerciseManifest
        {
            Id = "ex03",
            Grading = { new GradingStep { Type = "boom" } }
        };
        var grader = new ExerciseGrader(new IGrader[] { new ThrowingGrader() });

        var result = grader.Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        // Fail-closed élargi : une exception inattendue d'un grader (réseau, git…) ne remonte pas et ne
        // « réussit » pas — elle devient un échec interne, affiché mais NON persisté comme régression (M-10).
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        var failure = Assert.Single(result.Results);
        Assert.True(failure.IsInternalError);
        Assert.Contains(failure.Messages, m => m.Contains("erreur inattendue"));
        Assert.True(result.IsInternalError);
    }

    /// <summary>Grader factice qui lève une exception non liée au bac à sable (simule réseau/git en panne).</summary>
    private sealed class ThrowingGrader : IGrader
    {
        public string Type => "boom";

        public GraderResult Grade(GradingContext context, GradingStep step) =>
            throw new System.InvalidOperationException("panne simulée du grader");
    }
}
