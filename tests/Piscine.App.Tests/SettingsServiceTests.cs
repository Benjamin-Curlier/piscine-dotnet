using System.IO;
using Piscine.App.Settings;
using Piscine.Core;
using Xunit;

namespace Piscine.App.Tests;

public sealed class SettingsServiceTests
{
    private static SettingsService Create(TempDir tmp) =>
        new(new PiscineLayout(tmp.Path, Path.Combine(tmp.Path, "ws"), Path.Combine(tmp.Path, "state")));

    [Fact]
    public void Load_returns_default_when_file_absent()
    {
        using var tmp = new TempDir();
        Assert.Null(Create(tmp).Load().EditorCommand);
    }

    [Fact]
    public void Save_then_Load_roundtrips_editor_command()
    {
        using var tmp = new TempDir();
        var svc = Create(tmp);
        svc.Save(new AppSettings { EditorCommand = "code" });
        Assert.Equal("code", svc.Load().EditorCommand);
    }

    [Fact]
    public void Load_returns_default_when_file_corrupt()
    {
        using var tmp = new TempDir();
        var state = Path.Combine(tmp.Path, "state");
        Directory.CreateDirectory(state);
        File.WriteAllText(Path.Combine(state, "settings.json"), "{ pas du json");

        Assert.Null(Create(tmp).Load().EditorCommand); // pas d'exception
    }
}
