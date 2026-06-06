using System.Collections.Generic;
using LibGit2Sharp;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GitFixtureBuilderTests
{
    private static List<GitFixtureStep> BrancheEtFusion() => new()
    {
        new GitFixtureStep { Branch = "main", Message = "init", Files = new() { ["README.md"] = "Projet\n" } },
        new GitFixtureStep { Branch = "feature", Base = "main" },
        new GitFixtureStep { Branch = "feature", Message = "ajout", Files = new() { ["feature.txt"] = "salut\n" } },
        new GitFixtureStep { MergeInto = "main", MergeFrom = "feature" },
    };

    [Fact]
    public void Build_CreatesBranchesCommitsAndMerge()
    {
        using var dir = new TempDir();

        GitFixtureBuilder.Build(BrancheEtFusion(), dir.Path);

        using var repo = new Repository(dir.Path);
        Assert.NotNull(repo.Branches["main"]);
        Assert.NotNull(repo.Branches["feature"]);
        // Après fusion (fast-forward), main contient le fichier ajouté sur feature.
        Assert.NotNull(repo.Branches["main"].Tip["feature.txt"]);
    }

    [Fact]
    public void Build_ProducesRepoThatPassesGitGrader()
    {
        using var dir = new TempDir();
        GitFixtureBuilder.Build(BrancheEtFusion(), dir.Path);

        var step = new GradingStep
        {
            Type = "git",
            Git = new GitAssertions
            {
                Branches = { "main", "feature" },
                MinCommits = 2,
                NoConflictMarkers = true,
                Merged = { new GitMerge { Into = "main", Branch = "feature" } },
                Files = { new GitFileAssertion { Path = "feature.txt", Ref = "main", Contains = "salut" } },
            },
        };

        var result = new GitGrader().Grade(
            new GradingContext(new Dictionary<string, string>(), repositoryPath: dir.Path), step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Build_FirstCommitUsesNamedBranch()
    {
        using var dir = new TempDir();

        GitFixtureBuilder.Build(
            new List<GitFixtureStep>
            {
                new() { Branch = "main", Message = "init", Files = new() { ["a.txt"] = "1\n" } },
            },
            dir.Path);

        using var repo = new Repository(dir.Path);
        Assert.NotNull(repo.Branches["main"]);
        Assert.Equal("main", repo.Head.FriendlyName);
    }
}
