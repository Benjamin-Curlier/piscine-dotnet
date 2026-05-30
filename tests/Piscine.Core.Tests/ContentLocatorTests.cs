using System.IO;
using Piscine.Core;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentLocatorTests
{
    [Fact]
    public void FindExercise_ReturnsLocation_WhenExerciseExists()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");
        dir.WriteFile(Path.Combine("modules", "00-setup", "exercises", "ex00-hello", "manifest.yaml"), "id: ex00-hello\n");

        var location = ContentLocator.FindExercise(new PiscinePaths(dir.Path), "ex00-hello");

        Assert.NotNull(location);
        Assert.Equal("00-setup", location!.ModuleId);
        Assert.Equal("ex00-hello", location.ExerciseId);
        Assert.True(Directory.Exists(location.ContentDir));
    }

    [Fact]
    public void FindExercise_ReturnsNull_WhenMissing()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");

        var location = ContentLocator.FindExercise(new PiscinePaths(dir.Path), "inconnu");

        Assert.Null(location);
    }

    [Fact]
    public void FindExercise_FindsRush_UnderRushesModuleId()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("rushes", "r0-fizzbuzz", "manifest.yaml"), "id: r0-fizzbuzz\n");

        var location = ContentLocator.FindExercise(new PiscinePaths(dir.Path), "r0-fizzbuzz");

        Assert.NotNull(location);
        Assert.Equal(ContentLocator.RushesModuleId, location!.ModuleId);
        Assert.Equal("r0-fizzbuzz", location.ExerciseId);
        Assert.True(File.Exists(Path.Combine(location.ContentDir, "manifest.yaml")));
    }
}
