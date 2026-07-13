using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class NormeGraderTests
{
    private static readonly GradingStep Step = new() { Type = "norme", Blocking = false };

    [Fact]
    public void Grade_Advisory_OnPoorlyFormattedCode()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A{void M(){int x=1;}}"
        };

        var result = new NormeGrader().Grade(new GradingContext(sources),Step);

        Assert.Equal(GraderStatus.Reussi, result.Status); // non bloquant
        Assert.NotEmpty(result.Messages);
        // Le message doit être actionnable : oriente vers un reformatage concret.
        Assert.Contains(result.Messages, m => m.Contains("dotnet format"));
    }

    [Fact]
    public void Grade_NoMessages_OnCanonicalCode()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A\n{\n    void M()\n    {\n        int x = 1;\n    }\n}\n"
        };

        var result = new NormeGrader().Grade(new GradingContext(sources),Step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WhenBlockingAndPoorlyFormatted()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A{void M(){int x=1;}}"
        };

        var result = new NormeGrader().Grade(new GradingContext(sources),new GradingStep { Type = "norme", Blocking = true });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
    }
}
