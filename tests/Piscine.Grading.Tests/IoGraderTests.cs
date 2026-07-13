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

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("Hello, Piscine!"));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Reussi_WhenProgramWritesThenCallsEnvironmentExit()
    {
        // M-4 : un programme correct qui termine par Environment.Exit(0) ne doit pas perdre son stdout.
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Hello, Piscine!");
                System.Environment.Exit(0);
                """
        };

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("Hello, Piscine!"));

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

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("Hello, Piscine!"));

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

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("peu importe"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("compil", System.StringComparison.OrdinalIgnoreCase));
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }

    [Fact]
    public void Grade_ManyCompileErrors_AreCappedWithOverflowSummary()
    {
        // Une avalanche d'identifiants non déclarés : la moulinette doit plafonner l'affichage
        // pour que la première cause reste lisible, avec un résumé « … et N autre(s) erreur(s). ».
        var sources = new Dictionary<string, string>
        {
            ["Bad.cs"] = """
                System.Console.Write(a1);
                System.Console.Write(a2);
                System.Console.Write(a3);
                System.Console.Write(a4);
                System.Console.Write(a5);
                System.Console.Write(a6);
                System.Console.Write(a7);
                """
        };

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("peu importe"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        // En-tête + 5 erreurs plafonnées + ligne de dépassement = 7 messages au plus.
        Assert.True(result.Messages.Count <= 7, $"trop de messages : {result.Messages.Count}");
        Assert.Contains(result.Messages, m => m.Contains("autre(s) erreur(s)"));
    }

    [Fact]
    public void Grade_StdoutDiffers_SetsIoMismatchTrigger()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Bonjour");
                """
        };

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("Hello, Piscine!"));

        Assert.Equal(FeedbackTriggers.IoMismatch, result.Trigger);
    }

    [Fact]
    public void Grade_WrongExitCode_SetsExitCodeTrigger()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("ok");
                return 3;
                """
        };

        var result = new IoGrader().Grade(new GradingContext(sources), IoStep("ok", expectExit: 0));

        Assert.Equal(FeedbackTriggers.ExitCode, result.Trigger);
    }

    [Fact]
    public void Grade_ContentError_WhenNoCases()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = "System.Console.Write(\"ok\");"
        };
        var step = new GradingStep { Type = "io" }; // aucune case

        var result = new IoGrader().Grade(new GradingContext(sources), step);

        // Fail-closed : sans cas, un rendu qui compile ne doit pas « réussir » par défaut.
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }
}
