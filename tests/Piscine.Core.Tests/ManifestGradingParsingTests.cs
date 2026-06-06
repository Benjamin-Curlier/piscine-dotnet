using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ManifestGradingParsingTests
{
    [Fact]
    public void Parses_MutationStep_WithReferenceAndMutants()
    {
        const string yaml = """
            id: ex03-mutation
            title: "Mutation"
            objective: "Ecrire des tests qui attrapent les bugs."
            deliverables: [CompteTests.cs]
            grading:
              - type: mutation
                reference: reference/Compte.cs
                mutants:
                  - id: borne-egal
                    label: "Le retrait egal au solde n'est pas couvert."
                    find: "amount > balance"
                    replace: "amount >= balance"
            solution: [CompteTests.cs]
            """;

        var manifest = Piscine.Core.Io.YamlLoader.Deserialize<Piscine.Core.Model.ExerciseManifest>(yaml);
        var step = Assert.Single(manifest.Grading);

        Assert.Equal("mutation", step.Type);
        Assert.Equal("reference/Compte.cs", step.Reference);
        var mutant = Assert.Single(step.Mutants);
        Assert.Equal("borne-egal", mutant.Id);
        Assert.Equal("Le retrait egal au solde n'est pas couvert.", mutant.Label);
        Assert.Equal("amount > balance", mutant.Find);
        Assert.Equal("amount >= balance", mutant.Replace);
    }

    [Fact]
    public void Load_ParsesGradingStepsAndFeedback()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("ex00", "manifest.yaml"), """
            id: ex00-hello
            title: "Hello"
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - args: []
                    stdin: ""
                    expect_stdout: "Hello, Piscine!\n"
                    expect_exit: 0
              - type: norme
                blocking: false
            feedback:
              course_ref: "cours.md#hello-world"
              hints:
                - when: io_mismatch
                  message: "Verifie la casse et le retour a la ligne."
            """);

        var manifest = ExerciseManifestLoader.Load(dir.Combine("ex00"));

        Assert.Equal(2, manifest.Grading.Count);
        Assert.Equal("io", manifest.Grading[0].Type);
        Assert.Single(manifest.Grading[0].Cases);
        Assert.Equal("Hello, Piscine!\n", manifest.Grading[0].Cases[0].ExpectStdout);
        Assert.Equal(0, manifest.Grading[0].Cases[0].ExpectExit);
        Assert.Equal("norme", manifest.Grading[1].Type);
        Assert.False(manifest.Grading[1].Blocking);
        Assert.Equal("cours.md#hello-world", manifest.Feedback.CourseRef);
        Assert.Equal("io_mismatch", manifest.Feedback.Hints[0].When);
    }
}
