using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ProgramRunnerTests
{
    private static byte[] Compile(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["P.cs"] = source },
            OutputKind.ConsoleApplication).AssemblyBytes;

    [Fact]
    public void Run_ReturnsStdoutAndExitCode()
    {
        var run = ProgramRunner.Run(Compile("System.Console.Write(\"hi\"); return 4;"),
            Array.Empty<string>(), stdin: "");

        Assert.False(run.TimedOut);
        Assert.Equal("hi", run.Stdout);
        Assert.Equal(4, run.ExitCode);
        Assert.Null(run.Error);
    }

    [Fact]
    public void Run_ReportsException()
    {
        var run = ProgramRunner.Run(Compile("throw new System.InvalidOperationException(\"x\");"),
            Array.Empty<string>(), stdin: "");

        Assert.NotNull(run.Error);
        Assert.Equal("InvalidOperationException", run.Error!.TypeName);
    }

    [Fact]
    public void Run_InfiniteLoop_TimesOut_WithoutCorruptingNextRun()
    {
        // Boucle infinie qui écrit en continu : sous l'ancien modèle in-process, la tâche orpheline
        // survit au timeout et écrit dans le StringWriter de l'exécution SUIVANTE (contamination).
        var loop = Compile("""
            while (true) { System.Console.Write("X"); System.Threading.Thread.Sleep(1); }
            """);
        var clean = Compile("""System.Console.Write("propre");""");

        var first = ProgramRunner.Run(loop, Array.Empty<string>(), stdin: "", TimeSpan.FromMilliseconds(500));
        Assert.True(first.TimedOut);

        // Laisser une fenêtre pendant laquelle une éventuelle orpheline pourrait corrompre la suite.
        System.Threading.Thread.Sleep(300);

        var second = ProgramRunner.Run(clean, Array.Empty<string>(), stdin: "", TimeSpan.FromSeconds(5));
        Assert.False(second.TimedOut);
        Assert.Equal("propre", second.Stdout); // aucun "X" parasite
    }
}
