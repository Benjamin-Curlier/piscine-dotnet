using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentDiscoveryTests
{
    [Fact]
    public void DiscoverModules_ReturnsModulesOrderedByOrder()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "01-bases", "module.yaml"), """
            id: 01-bases
            title: "Bases C#"
            order: 1
            """);
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), """
            id: 00-setup
            title: "Setup"
            order: 0
            """);
        // Dossier parasite sans module.yaml : doit être ignoré.
        Directory.CreateDirectory(dir.Combine(Path.Combine("modules", "_brouillon")));

        var paths = new PiscinePaths(dir.Path);
        var modules = ContentDiscovery.DiscoverModules(paths).ToList();

        Assert.Equal(2, modules.Count);
        Assert.Equal("00-setup", modules[0].Id);
        Assert.Equal("01-bases", modules[1].Id);
    }

    [Fact]
    public void DiscoverModules_SkipsMalformedModuleYaml_WithoutThrowing()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "00-ok", "module.yaml"), """
            id: 00-ok
            title: "OK"
            order: 0
            """);
        // module.yaml malformé (YAML invalide) : ne doit PAS faire planter la découverte (UX recrue).
        dir.WriteFile(Path.Combine("modules", "01-casse", "module.yaml"), "id: [pas: du: yaml valide");

        var modules = ContentDiscovery.DiscoverModules(new PiscinePaths(dir.Path)).ToList();

        Assert.Single(modules);
        Assert.Equal("00-ok", modules[0].Id);
    }

    [Fact]
    public void DiscoverModules_ReturnsEmptyWhenModulesDirectoryMissing()
    {
        using var dir = new TempDir();
        var paths = new PiscinePaths(dir.Path);

        var modules = ContentDiscovery.DiscoverModules(paths);

        Assert.Empty(modules);
    }

    [Fact]
    public void DiscoverRushes_ReturnsRushesSortedById()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("rushes", "r1-inventaire", "manifest.yaml"), """
            id: r1-inventaire
            title: "Inventaire"
            """);
        dir.WriteFile(Path.Combine("rushes", "r0-fizzbuzz", "manifest.yaml"), """
            id: r0-fizzbuzz
            title: "FizzBuzz"
            """);
        // Dossier parasite sans manifest : ignoré.
        Directory.CreateDirectory(dir.Combine(Path.Combine("rushes", "_brouillon")));

        var rushes = ContentDiscovery.DiscoverRushes(new PiscinePaths(dir.Path)).ToList();

        Assert.Equal(2, rushes.Count);
        Assert.Equal("r0-fizzbuzz", rushes[0].Id);
        Assert.Equal("FizzBuzz", rushes[0].Title);
        Assert.Equal("r1-inventaire", rushes[1].Id);
    }

    [Fact]
    public void DiscoverRushes_ReturnsEmptyWhenRushesDirectoryMissing()
    {
        using var dir = new TempDir();

        var rushes = ContentDiscovery.DiscoverRushes(new PiscinePaths(dir.Path));

        Assert.Empty(rushes);
    }
}
