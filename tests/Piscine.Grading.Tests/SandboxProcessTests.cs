using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// Couvre les chemins de bord du lancement en processus enfant (<see cref="ProgramRunner"/> →
/// <c>SandboxProcess.Run</c>) : timeout + kill-tree, sortie anticipée via <c>Environment.Exit</c>
/// (ExitedEarly), et arrêt anormal sans <c>result.json</c>. Ces chemins isolent du code recrue non
/// fiable — une régression silencieuse y compromettrait le fail-closed (cf. audit COV-1).
/// </summary>
public class SandboxProcessTests
{
    private static byte[] CompileIo(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["P.cs"] = source },
            OutputKind.ConsoleApplication).AssemblyBytes;

    [Fact]
    public void Run_InfiniteLoop_TimesOut_AndIsKilled()
    {
        var bytes = CompileIo("while (true) { }");

        var outcome = ProgramRunner.Run(
            bytes, Array.Empty<string>(), stdin: "", timeout: TimeSpan.FromMilliseconds(500));

        Assert.True(outcome.TimedOut);
    }

    [Fact]
    public void Run_RecruitCallsEnvironmentExit_ReportsExitCode_NotCrash()
    {
        // Environment.Exit n'autorise pas un retour normal : SandboxEntry capte ProcessExit, écrit un
        // résultat partiel ExitedEarly, et le parent y recolle le code de sortie réel du processus.
        var bytes = CompileIo("System.Environment.Exit(42);");

        var outcome = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(outcome.TimedOut);
        Assert.Null(outcome.Error);
        Assert.Equal(42, outcome.ExitCode);
    }

    [Fact]
    public void Run_AbnormalTermination_NoResult_ReportsAbnormalStop()
    {
        // FailFast n'exécute PAS le hook ProcessExit → aucun result.json → le parent doit signaler un
        // arrêt anormal (et non un succès silencieux).
        var bytes = CompileIo("""System.Environment.FailFast("boom");""");

        var outcome = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(outcome.TimedOut);
        Assert.NotNull(outcome.Error);
        Assert.Equal("ArrêtAnormal", outcome.Error!.TypeName);
    }
}
