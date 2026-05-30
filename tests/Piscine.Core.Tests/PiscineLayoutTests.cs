using System.IO;
using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class PiscineLayoutTests
{
    [Fact]
    public void Layout_ExposesDerivedPaths()
    {
        var layout = new PiscineLayout("/c", "/home/ws", "/home/.state");

        Assert.Equal("/c", layout.ContentRoot);
        Assert.Equal(Path.Combine("/c", "modules"), layout.Content.ModulesDirectory);
        Assert.Equal("/home/ws", layout.WorkspaceRoot);
        Assert.Equal(Path.Combine("/home/.state", "progress.json"), layout.ProgressPath);
    }

    [Fact]
    public void WorkspaceExerciseDir_CombinesModuleAndExercise()
    {
        var layout = new PiscineLayout("/c", "/ws", "/s");

        Assert.Equal(
            Path.Combine("/ws", "00-setup", "ex00"),
            layout.WorkspaceExerciseDir("00-setup", "ex00"));
    }

    [Fact]
    public void RemoteRepoPath_IsUnderStateDir()
    {
        var layout = new PiscineLayout("/c", "/ws", "/home/.state");

        Assert.Equal(Path.Combine("/home/.state", "remote.git"), layout.RemoteRepoPath);
    }
}
