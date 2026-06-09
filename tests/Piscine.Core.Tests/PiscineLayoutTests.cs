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

    // #58 : le résolveur partagé (CLI/hook) ignorait PISCINE_WORKSPACE — que le GUI honore — d'où une
    // divergence de workspace. Ces tests verrouillent la parité.

    [Fact]
    public void FromEnvironment_HonorsPiscineWorkspace()
    {
        var prevHome = Environment.GetEnvironmentVariable("PISCINE_HOME");
        var prevWs = Environment.GetEnvironmentVariable("PISCINE_WORKSPACE");
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_HOME", Path.Combine("X:", "home"));
            Environment.SetEnvironmentVariable("PISCINE_WORKSPACE", Path.Combine("X:", "explicit-ws"));

            var layout = PiscineLayout.FromEnvironment();

            Assert.Equal(Path.Combine("X:", "explicit-ws"), layout.WorkspaceRoot);
            Assert.Equal(Path.Combine(Path.Combine("X:", "home"), ".state"), layout.StateDir);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_HOME", prevHome);
            Environment.SetEnvironmentVariable("PISCINE_WORKSPACE", prevWs);
        }
    }

    [Fact]
    public void FromEnvironment_DefaultsWorkspaceUnderHome_WhenWorkspaceUnset()
    {
        var prevHome = Environment.GetEnvironmentVariable("PISCINE_HOME");
        var prevWs = Environment.GetEnvironmentVariable("PISCINE_WORKSPACE");
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_HOME", Path.Combine("X:", "home2"));
            Environment.SetEnvironmentVariable("PISCINE_WORKSPACE", null);

            var layout = PiscineLayout.FromEnvironment();

            Assert.Equal(Path.Combine(Path.Combine("X:", "home2"), "workspace"), layout.WorkspaceRoot);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_HOME", prevHome);
            Environment.SetEnvironmentVariable("PISCINE_WORKSPACE", prevWs);
        }
    }
}
