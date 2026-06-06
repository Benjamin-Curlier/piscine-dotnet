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
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "subject.md"), "énoncé");
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
    public void Validate_MissingSubject_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");
        File.Delete(Path.Combine(dir.Combine("content"), "modules", "00-setup", "exercises", "ex00", "subject.md"));

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("subject.md manquant"));
    }

    [Fact]
    public void Validate_DuplicateExerciseIdAcrossModules_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");
        // Second module avec le MÊME identifiant d'exercice ex00.
        dir.WriteFile(Path.Combine("content", "modules", "01-autre", "module.yaml"), """
            id: 01-autre
            order: 1
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "01-autre", "exercises", "ex00", "manifest.yaml"), "id: ex00");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("plusieurs modules"));
    }

    [Fact]
    public void Validate_OrphanExerciseOnDisk_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");
        // ex99 existe sur disque mais n'est listé dans aucun groupe.
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex99", "manifest.yaml"), "id: ex99");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.Contains(report.Issues, i => i.ExerciseId == "ex99" && i.Message.Contains("orphelin"));
    }

    [Fact]
    public void Validate_ReferencedExercise_NotFlaggedAsOrphan()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.DoesNotContain(report.Issues, i => i.Message.Contains("orphelin"));
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

    private const string MutationReferenceImpl = """
        public class Compte
        {
            public int Solde { get; private set; } = 100;
            public bool Retirer(int montant)
            {
                if (montant <= Solde) { Solde -= montant; return true; }
                return false;
            }
        }
        """;

    private const string MutationManifest = """
        id: ex00
        deliverables: [CompteTests.cs]
        grading:
          - type: mutation
            reference: reference/Compte.cs
            mutants:
              - id: borne-egal
                label: "Le retrait egal au solde n'est pas couvert."
                find: "montant <= Solde"
                replace: "montant < Solde"
        """;

    // Suite modèle qui TUE le mutant (teste la borne égale, Retirer(100)).
    private const string MutationStrongSolution = """
        using Xunit;
        public class CompteTests
        {
            [Fact] public void Inferieur() { Assert.True(new Compte().Retirer(40)); }
            [Fact] public void EgalAuSolde() { Assert.True(new Compte().Retirer(100)); }
            [Fact] public void Superieur() { Assert.False(new Compte().Retirer(101)); }
        }
        """;

    // Suite modèle FAIBLE : ne teste pas la borne -> le mutant survit -> exo rejeté par la gate.
    private const string MutationWeakSolution = """
        using Xunit;
        public class CompteTests
        {
            [Fact] public void Inferieur() { Assert.True(new Compte().Retirer(40)); }
        }
        """;

    private static void WriteMutationExercise(TempDir dir, string solutionTests)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine(ExerciseDir, "manifest.yaml"), MutationManifest);
        dir.WriteFile(Path.Combine(ExerciseDir, "subject.md"), "énoncé");
        dir.WriteFile(Path.Combine(ExerciseDir, "reference", "Compte.cs"), MutationReferenceImpl);
        dir.WriteFile(Path.Combine(ExerciseDir, "solution", "CompteTests.cs"), solutionTests);
    }

    [Fact]
    public void Validate_AcceptsWellFormedMutationExercise()
    {
        using var dir = new TempDir();
        WriteMutationExercise(dir, MutationStrongSolution);

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.True(report.IsValid, string.Join(" | ", report.Issues.Select(i => i.Message)));
    }

    [Fact]
    public void Validate_RejectsMutationExercise_WhenSolutionDoesNotKillMutant()
    {
        using var dir = new TempDir();
        WriteMutationExercise(dir, MutationWeakSolution);

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("corrigé"));
    }
}
