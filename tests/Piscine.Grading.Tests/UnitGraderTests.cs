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
}
