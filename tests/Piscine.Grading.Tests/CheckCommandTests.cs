using System;
using System.IO;
using Piscine.Core;
using Piscine.Core.Progression;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class CheckCommandTests
{
    private static PiscineLayout Setup(TempDir dir, string deliverableContent)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """);
        dir.WriteFile(Path.Combine("ws", "00-setup", "ex00", "Hello.cs"), deliverableContent);
        return new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));
    }

    [Fact]
    public void Run_Reussi_ReturnsZero_AndRecordsProgress()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("ex00");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
    }

    [Fact]
    public void Run_ARevoir_ReturnsOne()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"non\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("ex00");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("cours.md#hello", result.Output);
    }

    [Fact]
    public void Run_EmptyWorkspace_ReturnsEducationalMessage_WithoutCompiling()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");
        // Aucun livrable rendu : on supprime le fichier déposé par Setup.
        File.Delete(dir.Combine(Path.Combine("ws", "00-setup", "ex00", "Hello.cs")));

        var result = new CheckCommand(layout, Graders.Default()).Run("ex00");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Aucun fichier rendu", result.Output);
        Assert.Contains("piscine start ex00", result.Output);
        Assert.DoesNotContain("Main", result.Output);
        // Soumission vide : la progression ne doit pas être enregistrée.
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.False(progress.Exercises.ContainsKey("ex00"));
    }

    [Fact]
    public void Run_UnknownExercise_ReturnsTwo()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("inconnu");

        Assert.Equal(2, result.ExitCode);
    }

    /// <summary>Horloge fixe injectable (LocalTimeZone = UTC) pour vérifier le seam TimeProvider (SK-2).</summary>
    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    [Fact]
    public void Run_RecordsInjectedClock_AsLastAttempt()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");
        var fixedTime = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        new CheckCommand(layout, Graders.Default(), new FixedClock(fixedTime)).Run("ex00");

        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(fixedTime, progress.Exercises["ex00"].LastAttempt!.Value);
    }
}
