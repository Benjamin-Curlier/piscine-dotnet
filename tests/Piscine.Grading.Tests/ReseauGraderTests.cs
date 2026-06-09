using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ReseauGraderTests
{
    // Programme recrue : se connecte au serveur de test (args[0]:args[1]), renvoie chaque ligne de
    // stdin et imprime l'écho reçu. Usings explicites (ImplicitUsings désactivé côté grader).
    private const string EchoClient = """
        using System.IO;
        using System.Net.Sockets;

        var host = args[0];
        var port = int.Parse(args[1]);
        using var client = new TcpClient(host, port);
        using var stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };
        var reader = new StreamReader(stream);
        string? ligne;
        while ((ligne = System.Console.ReadLine()) is not null)
        {
            writer.WriteLine(ligne);
            var echo = reader.ReadLine();
            System.Console.WriteLine($"Reçu : {echo}");
        }
        """;

    private static GradingContext Context(string source) =>
        new(new Dictionary<string, string> { ["Program.cs"] = source });

    private static GradingStep Step(params (string Stdin, string Expect)[] cases)
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "echo" } };
        foreach (var (stdin, expect) in cases)
        {
            step.Cases.Add(new IoCase { Stdin = stdin, ExpectStdout = expect, ExpectExit = 0 });
        }

        return step;
    }

    [Fact]
    public void Harness_EchoesLine()
    {
        using var harness = NetworkHarness.StartEcho();
        using var client = new TcpClient(harness.Host, harness.Port);
        using var stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };
        var reader = new StreamReader(stream);

        writer.WriteLine("ping");

        Assert.Equal("ping", reader.ReadLine());
    }

    [Fact]
    public void Grade_Reussi_WhenEchoMatches()
    {
        var result = new ReseauGrader().Grade(Context(EchoClient), Step(("bonjour\n", "Reçu : bonjour\n")));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Reussi_WithMultipleLines()
    {
        var result = new ReseauGrader().Grade(
            Context(EchoClient),
            Step(("un\ndeux\ntrois\n", "Reçu : un\nReçu : deux\nReçu : trois\n")));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenOutputDiffers()
    {
        var result = new ReseauGrader().Grade(Context(EchoClient), Step(("bonjour\n", "Reçu : autre\n")));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.IoMismatch, result.Trigger);
    }

    [Fact]
    public void Grade_ARevoir_WhenProgramDoesNotCompile()
    {
        var result = new ReseauGrader().Grade(
            Context("var x = ;"),
            Step(("x\n", "y\n")));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }

    [Fact]
    public void Grade_ContentError_WhenUnknownMode()
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "ftp" } };
        step.Cases.Add(new IoCase { Stdin = "x\n", ExpectStdout = "y\n", ExpectExit = 0 });

        var result = new ReseauGrader().Grade(Context(EchoClient), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Grade_ContentError_WhenNoCases()
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "echo" } }; // aucune case

        var result = new ReseauGrader().Grade(Context(EchoClient), step);

        // Fail-closed : sans cas, un rendu qui compile ne doit pas « réussir » par défaut.
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }
}
