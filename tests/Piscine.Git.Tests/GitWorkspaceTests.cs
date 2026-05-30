using System.IO;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class GitWorkspaceTests
{
    private static PiscineLayout Layout(TempDir dir) =>
        new(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

    [Fact]
    public void Initialize_CreatesBareWorkspaceOriginAndHook()
    {
        using var dir = new TempDir();
        var layout = Layout(dir);

        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine");

        Assert.True(Repository.IsValid(layout.WorkspaceRoot));
        Assert.True(Repository.IsValid(layout.RemoteRepoPath));

        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            var origin = repo.Network.Remotes["origin"];
            Assert.NotNull(origin);
        }

        var hook = Path.Combine(layout.RemoteRepoPath, "hooks", "post-receive");
        Assert.True(File.Exists(hook));
        Assert.Contains("grade-received", File.ReadAllText(hook));
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var dir = new TempDir();
        var layout = Layout(dir);

        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine");
        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine"); // ne doit pas lever

        using var repo = new Repository(layout.WorkspaceRoot);
        Assert.NotNull(repo.Network.Remotes["origin"]);
    }
}
