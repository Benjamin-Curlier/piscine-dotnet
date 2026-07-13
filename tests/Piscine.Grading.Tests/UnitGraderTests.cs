using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class UnitGraderTests
{
    private static readonly GradingStep Step = new() { Type = "unit", TestFiles = { "grader/Tests.cs" } };

    private static GradingContext Context(string learner)
    {
        var sources = new Dictionary<string, string> { ["Maths.cs"] = learner };
        var graderFiles = new Dictionary<string, string>
        {
            ["grader/Tests.cs"] = """
                using Xunit;

                public class MathsTests
                {
                    [Fact]
                    public void Add_ReturnsSum()
                    {
                        Assert.Equal(5, Maths.Add(2, 3));
                    }
                }
                """
        };
        return new GradingContext(sources, graderFiles);
    }

    [Fact]
    public void Grade_Reussi_WhenHiddenTestsPass()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a + b; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenHiddenTestsFail()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a - b; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WhenLearnerCodeDoesNotCompile()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a + ; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("compil", System.StringComparison.OrdinalIgnoreCase));
    }

    // Un livrable-concept top-level (instructions de haut niveau + type interne testable) doit être
    // compilé en exécutable par le grader (sinon CS8805) ; le point d'entrée n'est jamais exécuté.
    private static GradingContext TopLevelContext(string learner)
    {
        var sources = new Dictionary<string, string> { ["Programme.cs"] = learner };
        var graderFiles = new Dictionary<string, string>
        {
            ["grader/Tests.cs"] = """
                using Xunit;

                public class CompteurTests
                {
                    [Fact]
                    public void Incremente() => Assert.Equal(1, new Compteur().Incrementer());
                }
                """
        };
        return new GradingContext(sources, graderFiles);
    }

    [Fact]
    public void Grade_Reussi_ForTopLevelDeliverable()
    {
        // Programme top-level : sa boucle écrit sur stdout, mais le runner xunit ne lance PAS l'entry point.
        var context = TopLevelContext("""
            System.Console.WriteLine(new Compteur().Incrementer());

            class Compteur { private int _n; public int Incrementer() => ++_n; }
            """);

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_ForTopLevelDeliverable_WhenTechniqueWrong()
    {
        var context = TopLevelContext("""
            System.Console.WriteLine(new Compteur().Incrementer());

            class Compteur { private int _n; public int Incrementer() => _n; }
            """);

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
    }
}
