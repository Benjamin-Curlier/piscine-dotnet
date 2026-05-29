using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ExerciseManifestLoaderTests
{
    [Fact]
    public void Load_ParsesManifestStructuralFields()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("ex00-hello", "manifest.yaml"), """
            id: ex00-hello
            title: "Hello, Piscine"
            objective: "Afficher un message precis"
            deliverables: [Hello.cs]
            starter: [starter/README.md]
            """);

        var manifest = ExerciseManifestLoader.Load(dir.Combine("ex00-hello"));

        Assert.Equal("ex00-hello", manifest.Id);
        Assert.Equal("Hello, Piscine", manifest.Title);
        Assert.Equal(new[] { "Hello.cs" }, manifest.Deliverables);
        Assert.Equal(new[] { "starter/README.md" }, manifest.Starter);
    }
}
