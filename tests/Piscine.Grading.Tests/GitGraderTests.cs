using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GitGraderTests
{
    private static readonly Signature Sig = new("Test", "test@piscine.dev", DateTimeOffset.UtcNow);

    private static void Commit(Repository repo, string file, string content, string message)
    {
        File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, file), content);
        Commands.Stage(repo, "*");
        repo.Commit(message, Sig, Sig);
    }

    private static GradingContext Context(string repoPath) =>
        new(new Dictionary<string, string>(), repositoryPath: repoPath);

    private static GraderResult Grade(string repoPath, GitAssertions git) =>
        new GitGrader().Grade(Context(repoPath), new GradingStep { Type = "git", Git = git });

    /// <summary>Crée un dépôt avec deux commits sur la branche par défaut, renvoie son nom.</summary>
    private static string InitWithCommits(TempDir dir, out string defaultBranch)
    {
        Repository.Init(dir.Path);
        using var repo = new Repository(dir.Path);
        Commit(repo, "README.md", "Bonjour la piscine\n", "init");
        Commit(repo, "src.txt", "code\n", "ajout");
        defaultBranch = repo.Head.FriendlyName;
        return dir.Path;
    }

    [Fact]
    public void Grade_Reussi_WhenAllAssertionsHold()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out var main);

        var result = Grade(dir.Path, new GitAssertions
        {
            Branches = { main },
            MinCommits = 2,
            NoConflictMarkers = true,
            Files = { new GitFileAssertion { Path = "README.md", Contains = "Bonjour" } },
        });

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenBranchMissing()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out _);

        var result = Grade(dir.Path, new GitAssertions { Branches = { "feature-absente" } });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.GitState, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("feature-absente"));
    }

    [Fact]
    public void Grade_ARevoir_WhenNotEnoughCommits()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out _);

        var result = Grade(dir.Path, new GitAssertions { MinCommits = 5 });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("commit"));
    }

    [Fact]
    public void Grade_ARevoir_WhenFileMissing()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out _);

        var result = Grade(dir.Path, new GitAssertions
        {
            Files = { new GitFileAssertion { Path = "absent.txt", Contains = "x" } },
        });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("absent.txt"));
    }

    [Fact]
    public void Grade_ARevoir_WhenFileDoesNotContainSubstring()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out _);

        var result = Grade(dir.Path, new GitAssertions
        {
            Files = { new GitFileAssertion { Path = "README.md", Contains = "Au revoir" } },
        });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("Au revoir"));
    }

    [Fact]
    public void Grade_Reussi_WhenExactContentMatches_NormalizingLineEndings()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path);
        using (var repo = new Repository(dir.Path))
        {
            Commit(repo, "note.txt", "ligne1\nligne2\n", "init");
        }

        var result = Grade(dir.Path, new GitAssertions
        {
            // Contenu attendu fourni en \r\n : le grader normalise.
            Files = { new GitFileAssertion { Path = "note.txt", Content = "ligne1\r\nligne2\r\n" } },
        });

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Reussi_WhenBranchMerged()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path);
        using (var repo = new Repository(dir.Path))
        {
            Commit(repo, "a.txt", "1\n", "base");
            var main = repo.Head.FriendlyName;
            var feature = repo.CreateBranch("feature");
            Commands.Checkout(repo, feature);
            Commit(repo, "b.txt", "2\n", "feature work");
            Commands.Checkout(repo, repo.Branches[main]);
            repo.Merge(repo.Branches["feature"], Sig);

            var result = Grade(dir.Path, new GitAssertions
            {
                Merged = { new GitMerge { Into = main, Branch = "feature" } },
            });

            Assert.Equal(GraderStatus.Reussi, result.Status);
        }
    }

    [Fact]
    public void Grade_ARevoir_WhenBranchNotMerged()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path);
        string main;
        using (var repo = new Repository(dir.Path))
        {
            Commit(repo, "a.txt", "1\n", "base");
            main = repo.Head.FriendlyName;
            var feature = repo.CreateBranch("feature");
            Commands.Checkout(repo, feature);
            Commit(repo, "b.txt", "2\n", "feature work");
            Commands.Checkout(repo, repo.Branches[main]); // pas de merge
        }

        var result = Grade(dir.Path, new GitAssertions
        {
            Merged = { new GitMerge { Into = main, Branch = "feature" } },
        });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("fusionnée"));
    }

    [Fact]
    public void Grade_ARevoir_WhenConflictMarkersPresent()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path);
        using (var repo = new Repository(dir.Path))
        {
            const string conflicted = "debut\n<<<<<<< HEAD\nmoi\n=======\nautre\n>>>>>>> feature\nfin\n";
            Commit(repo, "conflit.txt", conflicted, "marqueurs laissés");
        }

        var result = Grade(dir.Path, new GitAssertions { NoConflictMarkers = true });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("conflit"));
    }

    [Fact]
    public void Grade_ARevoir_WhenNoValidRepository()
    {
        using var dir = new TempDir(); // dossier non initialisé

        var result = Grade(dir.Path, new GitAssertions { MinCommits = 1 });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.GitState, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("dépôt git valide"));
    }

    [Fact]
    public void Grade_ARevoir_WhenRepoInitializedButEmpty()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path); // dépôt valide mais HEAD non né (aucun commit)

        var result = Grade(dir.Path, new GitAssertions { MinCommits = 1 });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("aucun commit"));
    }

    [Fact]
    public void Grade_ARevoir_WhenConflictMarkersNotAllThreePresent()
    {
        using var dir = new TempDir();
        Repository.Init(dir.Path);
        using (var repo = new Repository(dir.Path))
        {
            // Doc qui parle des conflits sans en être un : pas les trois marqueurs en début de ligne.
            Commit(repo, "doc.md", "Les marqueurs <<<<<<< et >>>>>>> indiquent un conflit.\n", "doc");
        }

        var result = Grade(dir.Path, new GitAssertions { NoConflictMarkers = true });

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ContentError_WhenGitBlockMissing()
    {
        using var dir = new TempDir();
        InitWithCommits(dir, out _);

        var result = new GitGrader().Grade(Context(dir.Path), new GradingStep { Type = "git" });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", StringComparison.OrdinalIgnoreCase));
    }
}
