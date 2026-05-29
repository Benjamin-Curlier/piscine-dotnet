using System.IO;
using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class PiscinePathsTests
{
    [Fact]
    public void ModulesDirectory_IsModulesUnderRoot()
    {
        var paths = new PiscinePaths("/content");

        Assert.Equal(Path.Combine("/content", "modules"), paths.ModulesDirectory);
    }

    [Fact]
    public void RushesDirectory_IsRushesUnderRoot()
    {
        var paths = new PiscinePaths("/content");

        Assert.Equal(Path.Combine("/content", "rushes"), paths.RushesDirectory);
    }
}
