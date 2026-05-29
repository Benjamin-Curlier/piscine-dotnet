using Piscine.Core.Io;
using Piscine.Core.Model;
using Xunit;

namespace Piscine.Core.Tests;

public class YamlLoaderTests
{
    [Fact]
    public void Load_MapsUnderscoredKeysToProperties()
    {
        using var dir = new TempDir();
        var file = dir.WriteFile("manifest.yaml", """
            id: ex00-hello
            title: "Hello"
            objective: "Afficher un message"
            deliverables: [Hello.cs]
            """);

        var manifest = YamlLoader.Load<ExerciseManifest>(file);

        Assert.Equal("ex00-hello", manifest.Id);
        Assert.Equal("Hello", manifest.Title);
        Assert.Equal("Afficher un message", manifest.Objective);
        Assert.Equal(new[] { "Hello.cs" }, manifest.Deliverables);
    }
}
