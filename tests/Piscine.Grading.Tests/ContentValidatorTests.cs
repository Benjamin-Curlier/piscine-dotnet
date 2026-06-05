using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ContentValidatorTests
{
    private static PiscineLayout LayoutFor(TempDir dir) =>
        new(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

    private static void WriteExercise(TempDir dir, string manifestYaml, string? solutionHello)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), manifestYaml);
        if (solutionHello is not null)
        {
            dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "solution", "Hello.cs"), solutionHello);
        }
    }

    private const string IoManifest = """
        id: ex00
        deliverables: [Hello.cs]
        grading:
          - type: io
            cases:
              - expect_stdout: "ok"
                expect_exit: 0
        """;

    [Fact]
    public void Validate_EmptyContent_IsValid()
    {
        using var dir = new TempDir();
        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_SolutionPassesItsGraders_IsValid()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_SolutionFails_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"non\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("corrigé"));
    }

    [Fact]
    public void Validate_MissingSolutionDir_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, solutionHello: null);

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.Message.Contains("solution/"));
    }

    [Fact]
    public void Validate_MissingGraderFile_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: unit
                test_files: [grader/Tests.cs]
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.Message.Contains("grader/Tests.cs"));
    }

    private static readonly string ExerciseDir = Path.Combine("content", "modules", "00-setup", "exercises", "ex00");
    private static readonly string CoursePath = Path.Combine("content", "modules", "00-setup", "cours.md");

    [Fact]
    public void Validate_InvalidDifficulty_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            difficulty: extreme
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("difficulty invalide"));
    }

    [Fact]
    public void Validate_ValidDifficulty_NoDifficultyIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            difficulty: facile
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.DoesNotContain(report.Issues, i => i.Message.Contains("difficulty"));
    }

    [Fact]
    public void Validate_DeclaredStarterMissing_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            starter: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("starter déclaré mais manquant"));
    }

    [Fact]
    public void Validate_DeclaredStarterPresent_NoStarterIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            starter: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            """, "System.Console.Write(\"ok\");");
        dir.WriteFile(Path.Combine(ExerciseDir, "starter", "Hello.cs"), "// départ");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.DoesNotContain(report.Issues, i => i.Message.Contains("starter"));
    }

    [Fact]
    public void Validate_CourseRefAnchorResolves_NoIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """, "System.Console.Write(\"ok\");");
        dir.WriteFile(CoursePath, "# Cours\n## Hello {#hello}\n");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.DoesNotContain(report.Issues, i => i.Message.Contains("course_ref"));
    }

    [Fact]
    public void Validate_CourseRefAnchorMissing_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#inexistant"
            """, "System.Console.Write(\"ok\");");
        dir.WriteFile(CoursePath, "# Cours\n## Hello {#hello}\n");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("ancre #inexistant"));
    }

    [Fact]
    public void Validate_InvalidHintTrigger_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              hints:
                - when: pas_un_declencheur
                  message: "indice"
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("hint when invalide"));
    }

    [Fact]
    public void Validate_ValidHintTrigger_NoHintIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              hints:
                - when: io_mismatch
                  message: "indice"
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.DoesNotContain(report.Issues, i => i.Message.Contains("hint when"));
    }

    [Fact]
    public void Validate_CourseRefButNoCourseFile_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("introuvable pour le module"));
    }
}
