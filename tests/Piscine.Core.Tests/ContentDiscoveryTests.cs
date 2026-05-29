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
    public void DiscoverModules_ReturnsEmptyWhenModulesDirectoryMissing()
    {
        using var dir = new TempDir();
        var paths = new PiscinePaths(dir.Path);

        var modules = ContentDiscovery.DiscoverModules(paths);

        Assert.Empty(modules);
    }
}
