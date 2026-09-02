using System.IO;
using System.Linq;
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

    [Fact]
    public void CopyWithoutSolutions_OverRealContentTree_ShipsNoSolutionSegment()
    {
        // Invariant à valeur élevée : aucun corrigé (dossier solution/) ne doit jamais sortir dans le
        // paquet distribué. On l'épingle sur le VRAI arbre content/ du dépôt (pas une fixture), pour
        // attraper toute régression de HasSolutionSegment ou tout futur changement de disposition.
        var contentDir = FindRepoContentDir();
        if (contentDir is null)
        {
            return; // arbre content/ introuvable (build isolé) : rien à vérifier ici.
        }

        using var dir = new TempDir();
        var outDir = dir.Combine("out");
        ContentPackager.CopyWithoutSolutions(contentDir, outDir);

        var shipped = Directory.EnumerateFiles(outDir, "*", SearchOption.AllDirectories).ToList();
        var leaked = shipped
            .Select(f => Path.GetRelativePath(outDir, f))
            .Where(rel => rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(seg => seg.Equals("solution", System.StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(leaked.Count == 0, "Corrigés fuités dans le paquet : " + string.Join(", ", leaked));
        // Contrôle positif : du contenu utile a bien été copié (sinon le test passerait à vide).
        Assert.Contains(shipped, f => Path.GetFileName(f).Equals("subject.md", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CopyWithoutSolutions_RemovesStaleDestinationFilesBeforePackaging()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("src", "manifest.yaml"), "id: ex00");
        dir.WriteFile(Path.Combine("out", "ex", "solution", "Stale.cs"), "ancien corrigé");
        dir.WriteFile(Path.Combine("out", "obsolete.txt"), "ancienne version");

        ContentPackager.CopyWithoutSolutions(dir.Combine("src"), dir.Combine("out"));

        Assert.True(File.Exists(Path.Combine(dir.Combine("out"), "manifest.yaml")));
        Assert.False(File.Exists(Path.Combine(dir.Combine("out"), "obsolete.txt")));
        Assert.False(Directory.Exists(Path.Combine(dir.Combine("out"), "ex", "solution")));
    }

    [Theory]
    [InlineData("same")]
    [InlineData("destination-inside-source")]
    [InlineData("source-inside-destination")]
    public void CopyWithoutSolutions_RejectsOverlappingTrees(string scenario)
    {
        using var dir = new TempDir();
        var source = scenario == "source-inside-destination"
            ? dir.Combine(Path.Combine("root", "src"))
            : dir.Combine("src");
        var destination = scenario switch
        {
            "same" => source,
            "destination-inside-source" => Path.Combine(source, "out"),
            _ => dir.Combine("root"),
        };
        Directory.CreateDirectory(source);

        Assert.Throws<ArgumentException>(() => ContentPackager.CopyWithoutSolutions(source, destination));
    }

    private static string? FindRepoContentDir()
    {
        var d = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (d is not null)
        {
            if (File.Exists(Path.Combine(d.FullName, "Piscine.slnx")))
            {
                var content = Path.Combine(d.FullName, "content");
                return Directory.Exists(content) ? content : null;
            }
            d = d.Parent;
        }
        return null;
    }
}
