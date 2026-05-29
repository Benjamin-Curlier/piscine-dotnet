using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ModuleLoaderTests
{
    [Fact]
    public void Load_ParsesModuleWithGroupsAndOrderedExercises()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("00-setup-git", "module.yaml"), """
            id: 00-setup-git
            title: "Mise en place & Git"
            order: 0
            course: cours.md
            groups:
              - id: premiers-commits
                title: "Premiers commits"
                exercises: [ex00-hello, ex01-identite]
              - id: branches-fusion
                title: "Branches & fusion"
                exercises: [ex02-branche]
            """);

        var module = ModuleLoader.Load(dir.Combine("00-setup-git"));

        Assert.Equal("00-setup-git", module.Id);
        Assert.Equal(0, module.Order);
        Assert.Equal("cours.md", module.Course);
        Assert.Equal(2, module.Groups.Count);
        Assert.Equal("premiers-commits", module.Groups[0].Id);
        Assert.Equal(new[] { "ex00-hello", "ex01-identite" }, module.Groups[0].Exercises);
        Assert.Equal(new[] { "ex02-branche" }, module.Groups[1].Exercises);
    }
}
