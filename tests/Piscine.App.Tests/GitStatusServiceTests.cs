using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Piscine.App.Git;
using Piscine.Core.Model;
using Piscine.Grading;

namespace Piscine.App.Tests;

/// <summary>
/// Tests du <see cref="GitStatusService"/> : un depot reel par cas, materialise via
/// <see cref="GitFixtureBuilder"/> puis manipule directement avec LibGit2Sharp pour les etats
/// « vivants » (index, HEAD detache, remote en avance, marqueurs de conflit).
/// </summary>
public sealed class GitStatusServiceTests
{
    private static readonly Signature Author =
        new("Test", "test@piscine.dev", new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero));

    private static List<GitFixtureStep> SingleCommitOnMain() =>
    [
        new GitFixtureStep
        {
            Branch = "main",
            Message = "premier commit",
            Files = new Dictionary<string, string> { ["README.md"] = "Bonjour\n" },
        },
    ];

    [Fact]
    public void Read_NonGitFolder_ReturnsNotARepository()
    {
        // Arrange
        using var dir = new TempDir();

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.False(state.IsRepository);
        Assert.Null(state.CurrentBranch);
    }

    [Fact]
    public void Read_RepoWithCommit_ReportsBranchAndHasAnyCommit()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.True(state.IsRepository);
        Assert.True(state.HasAnyCommit);
        Assert.Equal("main", state.CurrentBranch);
        Assert.False(state.IsDetachedHead);
    }

    [Fact]
    public void Read_DetachedHead_ReportsDetachedAndNullBranch()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        using (var repo = new Repository(dir.Path))
        {
            var sha = repo.Head.Tip!.Sha;
            Commands.Checkout(repo, repo.Lookup<Commit>(sha));
        }

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.True(state.IsDetachedHead);
        Assert.Null(state.CurrentBranch);
        Assert.True(state.HasAnyCommit);
    }

    [Fact]
    public void Read_StagedFile_CountsAsStaged()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        File.WriteAllText(dir.Combine("nouveau.txt"), "contenu\n");
        using (var repo = new Repository(dir.Path))
        {
            Commands.Stage(repo, "nouveau.txt");
        }

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.Equal(1, state.StagedCount);
        Assert.Equal(0, state.UnstagedCount);
        Assert.True(state.HasUncommittedWork);
    }

    [Fact]
    public void Read_ModifiedTrackedFile_CountsAsUnstaged()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        File.WriteAllText(dir.Combine("README.md"), "Modifie\n");

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.Equal(0, state.StagedCount);
        Assert.Equal(1, state.UnstagedCount);
    }

    [Fact]
    public void Read_UntrackedFile_CountsAsUntracked()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        File.WriteAllText(dir.Combine("brouillon.txt"), "wip\n");

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.Equal(1, state.UntrackedCount);
        Assert.Equal(0, state.StagedCount);
        Assert.Equal(0, state.UnstagedCount);
    }

    [Fact]
    public void Read_NoRemote_ReportsHasOriginFalseAndAheadZero()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.False(state.HasOrigin);
        Assert.Equal(0, state.AheadOfOrigin);
    }

    [Fact]
    public void Read_LocalCommitAheadOfOrigin_ReportsAheadGreaterThanZero()
    {
        // Arrange : pousser le 1er commit vers un bare « origin », puis committer en local.
        using var work = new TempDir();
        using var origin = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), work.Path);
        Repository.Init(origin.Path, isBare: true);

        using (var repo = new Repository(work.Path))
        {
            var remote = repo.Network.Remotes.Add("origin", origin.Path);
            // Refspec explicite : cree refs/remotes/origin/main sans exiger d'upstream configure.
            repo.Network.Push(remote, "refs/heads/main:refs/heads/main");

            File.WriteAllText(work.Combine("README.md"), "Suite\n");
            Commands.Stage(repo, "README.md");
            repo.Commit("deuxieme commit", Author, Author);
        }

        // Act
        var state = new GitStatusService().Read(work.Path);

        // Assert
        Assert.True(state.HasOrigin);
        Assert.Equal(1, state.AheadOfOrigin);
    }

    [Fact]
    public void Read_ConflictMarkersAtLineStart_ReportsConflictedFile()
    {
        // Arrange
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        var conflicted =
            "ligne saine\n" +
            "<<<<<<< HEAD\n" +
            "ma version\n" +
            "=======\n" +
            "leur version\n" +
            ">>>>>>> autre\n";
        File.WriteAllText(dir.Combine("README.md"), conflicted);

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.Contains("README.md", state.ConflictedFiles);
    }

    [Fact]
    public void Read_MarkersNotAtLineStart_ReportsNoConflict()
    {
        // Arrange : une doc qui PARLE des marqueurs (non en debut de ligne) ne doit pas matcher.
        using var dir = new TempDir();
        GitFixtureBuilder.Build(SingleCommitOnMain(), dir.Path);
        var doc =
            "Un conflit affiche `<<<<<<<` puis `=======` puis `>>>>>>>`.\n" +
            "Resous-le en editant les sections entre ces marqueurs.\n";
        File.WriteAllText(dir.Combine("README.md"), doc);

        // Act
        var state = new GitStatusService().Read(dir.Path);

        // Assert
        Assert.Empty(state.ConflictedFiles);
    }
}
