using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class IoGraderTests
{
    private static GradingStep IoStep(string expectStdout, int expectExit = 0)
    {
        return new GradingStep
        {
            Type = "io",
            Cases = { new IoCase { ExpectStdout = expectStdout, ExpectExit = expectExit } }
        };
    }

    [Fact]
    public void Grade_Reussi_WhenStdoutMatches()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Hello, Piscine!");
                """
        };

        var result = new IoGrader().Grade(sources, IoStep("Hello, Piscine!"));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenStdoutDiffers()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Bonjour");
                """
        };

        var result = new IoGrader().Grade(sources, IoStep("Hello, Piscine!"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WithCompileErrors()
    {
        var sources = new Dictionary<string, string>
        {
            ["Bad.cs"] = "ceci ne compile pas"
        };

        var result = new IoGrader().Grade(sources, IoStep("peu importe"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("compil", System.StringComparison.OrdinalIgnoreCase));
    }
}
