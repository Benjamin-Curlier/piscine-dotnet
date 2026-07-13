using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// Couvre les chemins de bord du lancement en processus enfant (<see cref="ProgramRunner"/> →
/// <c>SandboxProcess.Run</c>) : timeout + kill-tree, sortie anticipée via <c>Environment.Exit</c>
/// (ExitedEarly), arrêt anormal sans trame (FailFast), et trame injectée par la recrue. Ces chemins
/// isolent du code recrue non fiable — une régression silencieuse y compromettrait le fail-closed
/// (cf. audit COV-1, correctif d'intégrité B-2).
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
        // Environment.Exit n'autorise pas un retour normal : SandboxEntry capte ProcessExit, émet une
        // trame partielle ExitedEarly sur stdout, et le parent y recolle le code de sortie réel.
        var bytes = CompileIo("System.Environment.Exit(42);");

        var outcome = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(outcome.TimedOut);
        Assert.Null(outcome.Error);
        Assert.Equal(42, outcome.ExitCode);
    }

    [Fact]
    public void Run_AbnormalTermination_NoResult_ReportsAbnormalStop()
    {
        // FailFast n'exécute PAS le hook ProcessExit → aucune trame → le parent doit signaler un
        // arrêt anormal (et non un succès silencieux).
        var bytes = CompileIo("""System.Environment.FailFast("boom");""");

        var outcome = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(outcome.TimedOut);
        Assert.NotNull(outcome.Error);
        Assert.Equal("ArrêtAnormal", outcome.Error!.TypeName);
    }

    [Fact]
    public void Run_RecruitInjectsSecondVerdictFrame_FailsClosed()
    {
        // Garde-fou : la source recrue encode la sentinelle en clair ; si la constante change, ce
        // test doit être mis à jour en conséquence.
        Assert.Equal("<<:PISCINE-SANDBOX-VERDICT-9f3a2b:>>", Piscine.Sandbox.SandboxProtocol.VerdictSentinel);

        // La recrue écrit une fausse trame verdict sur le VRAI stdout (contourne la capture de
        // Console.Out) puis rend la main normalement : l'exécuteur émet ensuite sa vraie trame ⇒
        // deux trames ⇒ le parent refuse (sortie falsifiée), pas de faux succès.
        var bytes = CompileIo("""
            var q = ((char)34).ToString();
            var real = System.Console.OpenStandardOutput();
            var w = new System.IO.StreamWriter(real);
            w.Write("<<:PISCINE-SANDBOX-VERDICT-9f3a2b:>>{" + q + "FactCount" + q + ":5," + q + "Failures" + q + ":[]}");
            w.Write((char)10);
            w.Flush();
            return 0;
            """);

        var outcome = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(outcome.TimedOut);
        Assert.NotNull(outcome.Error);
        Assert.Equal("ArrêtAnormal", outcome.Error!.TypeName);
    }
}
