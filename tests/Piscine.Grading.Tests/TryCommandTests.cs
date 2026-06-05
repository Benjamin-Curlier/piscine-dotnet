using System.IO;
using Piscine.Core;
using Xunit;

namespace Piscine.Grading.Tests;

public class TryCommandTests
{
    private static PiscineLayout LayoutFor(TempDir dir) =>
        new(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

    private static void WriteExercise(TempDir dir, string manifestYaml, string? solution)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), manifestYaml);
        if (solution is not null)
        {
            dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "solution", "Hello.cs"), solution);
        }
    }

    private const string IoManifest = """
        id: ex00
        deliverables: [Hello.cs]
        grading:
          - type: io
            cases:
              - stdin: ""
                expect_stdout: "ok"
                expect_exit: 0
        """;

    [Fact]
    public void Run_MissingExercise_Returns2()
    {
        using var dir = new TempDir();
        var result = new TryCommand(LayoutFor(dir)).Run("ex99");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("introuvable", result.Output);
    }

    [Fact]
    public void Run_SolutionMatchesManifest_Returns0AndPrintsRealStdout()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");

        var result = new TryCommand(LayoutFor(dir)).Run("ex00");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("expect_stdout : \"ok\"", result.Output);
        Assert.Contains("✓", result.Output);
    }

    [Fact]
    public void Run_StdoutDiffersFromManifest_Returns1AndPrintsPasteableRealOutput()
    {
        using var dir = new TempDir();
        // Le manifest attend "ok" mais le corrigé produit autre chose : try doit imprimer
        // le stdout RÉEL prêt à coller, et signaler l'écart.
        WriteExercise(dir, IoManifest, "System.Console.Write(\"bonjour\");");

        var result = new TryCommand(LayoutFor(dir)).Run("ex00");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("expect_stdout : \"bonjour\"", result.Output);
        Assert.Contains("diffère du manifest", result.Output);
    }

    [Fact]
    public void Run_SolutionDoesNotCompile_Returns1WithErrors()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "this is not valid C#");

        var result = new TryCommand(LayoutFor(dir)).Run("ex00");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("ne compile pas", result.Output);
    }

    private static string ManifestPath(TempDir dir) =>
        Path.Combine(dir.Combine("content"), "modules", "00-setup", "exercises", "ex00", "manifest.yaml");

    [Fact]
    public void Run_Write_RewritesExpectStdoutInManifest()
    {
        using var dir = new TempDir();
        // expect_stdout faux dans le manifest ; un commentaire à préserver.
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - stdin: "entree\n"  # commentaire auteur
                    expect_stdout: "faux"
                    expect_exit: 0
            """, "System.Console.Write(\"bonjour\");");

        var result = new TryCommand(LayoutFor(dir)).Run("ex00", write: true);
        var manifest = File.ReadAllText(ManifestPath(dir));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Manifest mis à jour", result.Output);
        Assert.Contains("expect_stdout: \"bonjour\"", manifest);
        Assert.DoesNotContain("\"faux\"", manifest);
        // stdin et commentaire de l'auteur préservés.
        Assert.Contains("stdin: \"entree\\n\"  # commentaire auteur", manifest);
    }

    [Fact]
    public void Run_Write_RewritesExitCode()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - stdin: ""
                    expect_stdout: "ok"
                    expect_exit: 0
            """, "System.Console.Write(\"ok\");\nreturn 7;");

        new TryCommand(LayoutFor(dir)).Run("ex00", write: true);
        var manifest = File.ReadAllText(ManifestPath(dir));

        Assert.Contains("expect_exit: 7", manifest);
    }

    [Fact]
    public void Run_NewlineAndQuotesEscapedInYamlForm()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"{\\\"k\\\":1}\\n\");");

        var result = new TryCommand(LayoutFor(dir)).Run("ex00");

        // Forme YAML collable : \n littéral et guillemets échappés.
        Assert.Contains("expect_stdout : \"{\\\"k\\\":1}\\n\"", result.Output);
    }
}
