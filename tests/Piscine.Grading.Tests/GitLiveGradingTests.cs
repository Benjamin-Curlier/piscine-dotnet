using System.Collections.Generic;
using LibGit2Sharp;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// #17 — notation live des exos git côté <c>grade-received</c> : le grader doit savoir noter un
/// **dépôt bare** (HEAD orphelin après push) via <c>HeadRef</c>, et l'évaluateur « tenté » doit
/// ne déclencher la notation que sur un exo réellement commencé.
/// </summary>
public class GitLiveGradingTests
{
    private static List<GitFixtureStep> BrancheEtFusion() => new()
    {
        new GitFixtureStep { Branch = "main", Message = "init", Files = new() { ["README.md"] = "Projet\n" } },
        new GitFixtureStep { Branch = "feature", Base = "main" },
        new GitFixtureStep { Branch = "feature", Message = "ajout", Files = new() { ["feature.txt"] = "salut\n" } },
        new GitFixtureStep { MergeInto = "main", MergeFrom = "feature" },
    };

    /// <summary>Construit un dépôt corrigé puis pousse <paramref name="refs"/> dans un bare ; renvoie le bare.</summary>
    private static string BuildBareAfterPush(TempDir dir, params string[] refs)
    {
        var workPath = dir.Combine("work");
        GitFixtureBuilder.Build(BrancheEtFusion(), workPath);

        var barePath = dir.Combine("bare.git");
        Repository.Init(barePath, isBare: true);

        using var work = new Repository(workPath);
        work.Network.Remotes.Add("origin", barePath);
        var specs = new List<string>();
        foreach (var r in refs)
        {
            specs.Add($"refs/heads/{r}:refs/heads/{r}");
        }

        work.Network.Push(work.Network.Remotes["origin"], specs, new PushOptions());
        return barePath;
    }

    private static GradingStep GitStep() => new()
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

    [Fact]
    public void Grade_BareRepo_WithHeadRefMain_Passes()
    {
        using var dir = new TempDir();
        var bare = BuildBareAfterPush(dir, "main", "feature");

        var result = new GitGrader().Grade(
            new GradingContext(new Dictionary<string, string>(), repositoryPath: bare, headRef: "main"),
            GitStep());

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_BareRepo_WithoutHeadRef_FailsMinCommits_ProvingHeadRefNeeded()
    {
        using var dir = new TempDir();
        var bare = BuildBareAfterPush(dir, "main", "feature");

        // On force le HEAD symbolique du bare vers une branche inexistante pour rendre repo.Head
        // orphelin de façon déterministe. Sinon le HEAD pointe vers init.defaultBranch de la machine :
        // « master » (orphelin, car non poussé) ou « main » (résolu après le push) — d'où un test
        // dépendant de l'environnement. Ici la condition « aucun HEAD résoluble » est garantie.
        System.IO.File.WriteAllText(System.IO.Path.Combine(bare, "HEAD"), "ref: refs/heads/nonexistent\n");

        // Sans HeadRef, le grader lit repo.Head — orphelin dans un bare → « aucun commit ».
        var result = new GitGrader().Grade(
            new GradingContext(new Dictionary<string, string>(), repositoryPath: bare),
            GitStep());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("commit"));
    }

    [Fact]
    public void IsAttempted_NullAttempt_ReturnsFalse()
    {
        using var dir = new TempDir();
        var bare = BuildBareAfterPush(dir, "main", "feature");

        Assert.False(GitAttemptEvaluator.IsAttempted(null, bare));
    }

    [Fact]
    public void IsAttempted_BranchPresent_ReturnsTrue()
    {
        using var dir = new TempDir();
        var bare = BuildBareAfterPush(dir, "main", "feature");

        Assert.True(GitAttemptEvaluator.IsAttempted(new GitAttempt { Branch = "feature" }, bare));
    }

    [Fact]
    public void IsAttempted_BranchAbsent_ReturnsFalse()
    {
        using var dir = new TempDir();
        // feature non poussée : seul main est dans le bare.
        var bare = BuildBareAfterPush(dir, "main");

        Assert.False(GitAttemptEvaluator.IsAttempted(new GitAttempt { Branch = "feature" }, bare));
    }

    [Fact]
    public void IsAttempted_FilePresent_ReturnsTrue()
    {
        using var dir = new TempDir();
        var bare = BuildBareAfterPush(dir, "main", "feature");

        var attempt = new GitAttempt { File = new GitFileAssertion { Path = "feature.txt", Ref = "main" } };
        Assert.True(GitAttemptEvaluator.IsAttempted(attempt, bare));
    }

    [Fact]
    public void IsAttempted_InvalidRepo_ReturnsFalse()
    {
        using var dir = new TempDir();
        Assert.False(GitAttemptEvaluator.IsAttempted(new GitAttempt { Branch = "feature" }, dir.Combine("nope")));
    }
}
