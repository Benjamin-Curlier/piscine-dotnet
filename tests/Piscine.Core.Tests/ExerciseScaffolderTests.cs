using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ExerciseScaffolderTests
{
    [Fact]
    public void Create_GeneratesExerciseSkeleton()
    {
        using var dir = new TempDir();
        // Le module doit déjà exister (on n'ajoute qu'un exercice).
        Directory.CreateDirectory(dir.Combine(Path.Combine("modules", "02-boucles")));

        var exerciseDir = ExerciseScaffolder.Create(dir.Combine("modules"), "02-boucles", "ex03-compte-rebours");

        Assert.True(File.Exists(Path.Combine(exerciseDir, "manifest.yaml")));
        Assert.True(File.Exists(Path.Combine(exerciseDir, "subject.md")));
        Assert.True(File.Exists(Path.Combine(exerciseDir, "starter", "CompteRebours.cs")));
        Assert.True(File.Exists(Path.Combine(exerciseDir, "solution", "CompteRebours.cs")));

        var manifest = File.ReadAllText(Path.Combine(exerciseDir, "manifest.yaml"));
        Assert.Contains("id: ex03-compte-rebours", manifest);
        Assert.Contains("CompteRebours.cs", manifest);
        Assert.Contains("type: io", manifest);
    }

    [Theory]
    [InlineData("ex00-somme", "Somme.cs")]
    [InlineData("ex02-somme-n", "SommeN.cs")]
    [InlineData("ex10-compte-rebours", "CompteRebours.cs")]
    [InlineData("hello", "Hello.cs")]
    public void DeliverableFileName_DerivesPascalCaseFromId(string exerciseId, string expected)
    {
        Assert.Equal(expected, ExerciseScaffolder.DeliverableFileName(exerciseId));
    }

    [Fact]
    public void Create_ExistingExercise_Throws()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Combine(Path.Combine("modules", "02-boucles", "exercises", "ex03-x")));

        Assert.Throws<IOException>(
            () => ExerciseScaffolder.Create(dir.Combine("modules"), "02-boucles", "ex03-x"));
    }

    [Fact]
    public void Create_UnknownModule_Throws()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Combine("modules"));

        Assert.Throws<DirectoryNotFoundException>(
            () => ExerciseScaffolder.Create(dir.Combine("modules"), "99-absent", "ex00-x"));
    }
}
