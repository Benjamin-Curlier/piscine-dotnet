using System.IO;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class SubmissionLoaderTests
{
    [Fact]
    public void Load_ReadsDeliverablesFromWorkspaceAndGraderFilesFromContent()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: unit
                test_files: [grader/Tests.cs]
            """);
        dir.WriteFile(Path.Combine("content", "grader", "Tests.cs"), "// tests caches");
        dir.WriteFile(Path.Combine("ws", "Hello.cs"), "// mon code");

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));

        Assert.Equal("ex00", submission.Manifest.Id);
        Assert.Equal("// mon code", submission.Context.Sources["Hello.cs"]);
        Assert.Equal("// tests caches", submission.Context.GraderFiles["grader/Tests.cs"]);
    }

    [Fact]
    public void Load_OmitsMissingDeliverables()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex00
            deliverables: [Absent.cs]
            """);
        Directory.CreateDirectory(dir.Combine("ws"));

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));

        Assert.Empty(submission.Context.Sources);
    }

    [Fact]
    public void Load_PutsReferenceIntoGraderFiles()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex03-mutation
            title: "Mutation"
            objective: "x"
            deliverables: [CompteTests.cs]
            grading:
              - type: mutation
                reference: reference/Compte.cs
                mutants:
                  - id: m
                    label: "l"
                    find: "a"
                    replace: "b"
            solution: [CompteTests.cs]
            """);
        dir.WriteFile(Path.Combine("content", "reference", "Compte.cs"), "public class Compte { }");
        dir.WriteFile(Path.Combine("ws", "CompteTests.cs"), "// tests");

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));

        Assert.True(submission.Context.GraderFiles.ContainsKey("reference/Compte.cs"));
        Assert.Contains("public class Compte", submission.Context.GraderFiles["reference/Compte.cs"]);
    }

    [Fact]
    public void Load_RemovesFileBasedDirectives_ButPreservesDiagnosticLines()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex00
            deliverables: [Program.cs]
            """);
        dir.WriteFile(Path.Combine("ws", "Program.cs"), """
            #!
            #:property PublishAot=false
            #:package Exemple@1.0.0
            #:include .\Domain\Modele.cs
            System.Console.WriteLine("ok");
            """);

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));
        var source = submission.Context.Sources["Program.cs"];

        Assert.DoesNotContain("#:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("#!", source, StringComparison.Ordinal);
        Assert.Equal(5, source.Split('\n', StringSplitOptions.None)
            .Select((line, index) => (line, index))
            .Single(x => x.line.Contains("WriteLine", StringComparison.Ordinal)).index + 1);
    }
}
