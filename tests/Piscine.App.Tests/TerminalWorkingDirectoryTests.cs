using Piscine.App.Terminal;
using Piscine.Core;

namespace Piscine.App.Tests;

public sealed class TerminalWorkingDirectoryTests
{
    [Fact]
    public void Resolve_WithoutRequest_UsesOfficialWorkspace()
    {
        using var temp = new TempDir();
        var workspace = temp.Combine("workspace");
        Directory.CreateDirectory(workspace);
        var layout = new PiscineLayout(temp.Combine("content"), workspace, temp.Combine("state"));

        Assert.Equal(Path.GetFullPath(workspace), TerminalWorkingDirectory.Resolve(layout, null));
    }

    [Fact]
    public void Resolve_ExistingExerciseDirectoryInsideWorkspace_UsesRequest()
    {
        using var temp = new TempDir();
        var workspace = temp.Combine("workspace");
        var exercise = Path.Combine(workspace, "00-setup", "ex00");
        Directory.CreateDirectory(exercise);
        var layout = new PiscineLayout(temp.Combine("content"), workspace, temp.Combine("state"));

        Assert.Equal(Path.GetFullPath(exercise), TerminalWorkingDirectory.Resolve(layout, exercise));
    }

    [Theory]
    [InlineData("outside")]
    [InlineData("missing-inside")]
    public void Resolve_InvalidRequest_FallsBackToWorkspace(string scenario)
    {
        using var temp = new TempDir();
        var workspace = temp.Combine("workspace");
        Directory.CreateDirectory(workspace);
        var requested = scenario == "outside"
            ? temp.Combine("outside")
            : Path.Combine(workspace, "missing");
        Directory.CreateDirectory(temp.Combine("outside"));
        var layout = new PiscineLayout(temp.Combine("content"), workspace, temp.Combine("state"));

        Assert.Equal(Path.GetFullPath(workspace), TerminalWorkingDirectory.Resolve(layout, requested));
    }
}
