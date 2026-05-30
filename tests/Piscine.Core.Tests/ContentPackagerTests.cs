using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentPackagerTests
{
    [Fact]
    public void CopyWithoutSolutions_CopiesContent_ButOmitsSolutionDirs()
    {
        using var dir = new TempDir();
        var exo = Path.Combine("src", "modules", "00", "exercises", "ex00");
        dir.WriteFile(Path.Combine(exo, "manifest.yaml"), "id: ex00");
        dir.WriteFile(Path.Combine(exo, "subject.md"), "énoncé");
        dir.WriteFile(Path.Combine(exo, "starter", "README.md"), "départ");
        dir.WriteFile(Path.Combine(exo, "solution", "Hello.cs"), "// corrigé secret");

        ContentPackager.CopyWithoutSolutions(dir.Combine("src"), dir.Combine("out"));

        var outExo = Path.Combine(dir.Combine("out"), "modules", "00", "exercises", "ex00");
        Assert.True(File.Exists(Path.Combine(outExo, "manifest.yaml")));
        Assert.True(File.Exists(Path.Combine(outExo, "subject.md")));
        Assert.True(File.Exists(Path.Combine(outExo, "starter", "README.md")));
        Assert.False(Directory.Exists(Path.Combine(outExo, "solution")));
    }

    [Fact]
    public void CopyWithoutSolutions_OmitsFileNamedLikeButNestedUnderSolution_Only()
    {
        using var dir = new TempDir();
        // Un fichier "solution.md" (pas un dossier solution/) doit être conservé.
        dir.WriteFile(Path.Combine("src", "solution.md"), "doc");
        dir.WriteFile(Path.Combine("src", "ex", "solution", "S.cs"), "secret");

        ContentPackager.CopyWithoutSolutions(dir.Combine("src"), dir.Combine("out"));

        Assert.True(File.Exists(Path.Combine(dir.Combine("out"), "solution.md")));
        Assert.False(Directory.Exists(Path.Combine(dir.Combine("out"), "ex", "solution")));
    }
}
