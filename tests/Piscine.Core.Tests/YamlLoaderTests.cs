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

    [Fact]
    public void DeserializeStrict_RejectsUnknownKey_WhileLenientIgnoresIt()
    {
        const string yaml = "id: ex00\nexpext_stdout: oops\n"; // clé inconnue (typo)

        // Lenient (chargement runtime) : la clé inconnue est silencieusement ignorée.
        var lenient = YamlLoader.Deserialize<ExerciseManifest>(yaml);
        Assert.Equal("ex00", lenient.Id);

        // Strict (gate validate-content) : la même clé inconnue lève.
        Assert.NotNull(Record.Exception(() => YamlLoader.DeserializeStrict<ExerciseManifest>(yaml)));
    }
}
