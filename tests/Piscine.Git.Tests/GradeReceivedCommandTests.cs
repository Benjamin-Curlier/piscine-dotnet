using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Grading;
using Xunit;

namespace Piscine.Git.Tests;

public class GradeReceivedCommandTests
{
    private static PiscineLayout SetupContent(TempDir dir)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """);
        return new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));
    }

    // Crée le dépôt bare + un commit contenant <module>/<exo>/Hello.cs, renvoie le sha.
    private static string PushSnapshot(PiscineLayout layout, TempDir dir, string helloBody)
    {
        Repository.Init(layout.RemoteRepoPath, isBare: true);
        var workPath = dir.Combine("clone");
        Repository.Clone(layout.RemoteRepoPath, workPath);

        var file = Path.Combine(workPath, "00-setup", "ex00", "Hello.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, helloBody);

        using var repo = new Repository(workPath);
        Commands.Stage(repo, "*");
        var sig = new Signature("recrue", "r@piscine", DateTimeOffset.Now);
        var commit = repo.Commit("rendu", sig, sig);

        var branch = repo.Head.FriendlyName;
        var origin = repo.Network.Remotes["origin"];
        repo.Network.Push(origin, $"refs/heads/{branch}:refs/heads/{branch}", new PushOptions());
        return commit.Sha;
    }

    [Fact]
    public void Run_Reussi_ReturnsZero_AndRecordsProgress()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"ok\");");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
    }

    [Fact]
    public void Run_ARevoir_ReturnsOne_AndShowsCourseRef()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"non\");");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("À revoir", result.Output);
        Assert.Contains("cours.md#hello", result.Output);
    }

    [Fact]
    public void Run_GradesRush_PushedUnderRushesDir()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "rushes", "r0-demo", "manifest.yaml"), """
            id: r0-demo
            deliverables: [Demo.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "subject.md"
            """);
        var layout = new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

        Repository.Init(layout.RemoteRepoPath, isBare: true);
        var workPath = dir.Combine("clone");
        Repository.Clone(layout.RemoteRepoPath, workPath);
        var file = Path.Combine(workPath, "rushes", "r0-demo", "Demo.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "System.Console.Write(\"ok\");");
        using var repo = new Repository(workPath);
        Commands.Stage(repo, "*");
        var sig = new Signature("recrue", "r@piscine", DateTimeOffset.Now);
        var commit = repo.Commit("rush", sig, sig);
        var branch = repo.Head.FriendlyName;
        repo.Network.Push(repo.Network.Remotes["origin"], $"refs/heads/{branch}:refs/heads/{branch}", new PushOptions());

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(commit.Sha);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["r0-demo"].Status);
    }

    [Fact]
    public void Run_ARevoir_PersistsRichResult_WithDiffAndCourseRef()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"non\");");

        new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        // #40 : le verdict riche est persisté en plus de progress.json.
        var doc = new LastPushResultStore(layout.LastPushResultPath).Load();
        Assert.NotNull(doc);
        var ex = Assert.Single(doc!.Exercises);
        Assert.Equal("ex00", ex.ExerciseId);
        Assert.Equal("00-setup", ex.ModuleId);
        Assert.Equal("ARevoir", ex.Status);
        Assert.Equal("cours.md#hello", ex.CourseRef);
        var io = Assert.Single(ex.Cases);
        Assert.Equal("io", io.GraderType);
        Assert.False(io.Passed);
        // Le diff attendu/obtenu est bien capturé (et non plus perdu sur stdout du hook).
        Assert.Contains(io.Messages, m => m.Contains("Attendu") || m.Contains("Obtenu"));
    }

    [Fact]
    public void Run_Reussi_PersistsRichResult_StatusReussi()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"ok\");");

        new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        var doc = new LastPushResultStore(layout.LastPushResultPath).Load();
        Assert.NotNull(doc);
        var ex = Assert.Single(doc!.Exercises);
        Assert.Equal("ex00", ex.ExerciseId);
        Assert.Equal("Reussi", ex.Status);
    }

    // ── #17 : notation live des exos git contre le dépôt bare, gardée par le signal « tenté » ──────

    private static PiscineLayout SetupGitContent(TempDir dir)
    {
        dir.WriteFile(Path.Combine("content", "modules", "05-git", "module.yaml"), """
            id: 05-git
            order: 5
            groups:
              - id: g
                exercises: [ex-git]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "05-git", "exercises", "ex-git", "manifest.yaml"), """
            id: ex-git
            deliverables: []
            grading:
              - type: git
                git:
                  branches: [main, feature]
                  min_commits: 2
                  no_conflict_markers: true
                  merged:
                    - { into: main, branch: feature }
                  files:
                    - { path: feature.txt, ref: main, contains: "salut" }
                  attempt:
                    branch: feature
            feedback:
              hints:
                - when: git_state
                  message: "Crée la branche feature, commite, puis fusionne dans main."
              course_ref: "cours.md#merge"
            """);
        return new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));
    }

    private static List<GitFixtureStep> BrancheEtFusion() => new()
    {
        new GitFixtureStep { Branch = "main", Message = "init", Files = new() { ["README.md"] = "Projet\n" } },
        new GitFixtureStep { Branch = "feature", Base = "main" },
        new GitFixtureStep { Branch = "feature", Message = "ajout", Files = new() { ["feature.txt"] = "salut\n" } },
        new GitFixtureStep { MergeInto = "main", MergeFrom = "feature" },
    };

    // Construit le dépôt de la recrue (steps), pousse <refs> vers le bare, renvoie le sha de main.
    private static string BuildAndPush(PiscineLayout layout, TempDir dir, List<GitFixtureStep> steps, params string[] refs)
    {
        var workPath = dir.Combine("work");
        GitFixtureBuilder.Build(steps, workPath);
        Repository.Init(layout.RemoteRepoPath, isBare: true);
        using var repo = new Repository(workPath);
        repo.Network.Remotes.Add("origin", layout.RemoteRepoPath);
        var specs = refs.Select(r => $"refs/heads/{r}:refs/heads/{r}").ToList();
        repo.Network.Push(repo.Network.Remotes["origin"], specs, new PushOptions());
        return repo.Branches["main"].Tip.Sha;
    }

    [Fact]
    public void Run_GitExercise_BranchPushedAndMerged_ReussiAndRecorded()
    {
        using var dir = new TempDir();
        var layout = SetupGitContent(dir);
        var sha = BuildAndPush(layout, dir, BrancheEtFusion(), "main", "feature");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex-git"].Status);
    }

    [Fact]
    public void Run_GitExercise_NotAttempted_IsSkipped_NoSpuriousReview()
    {
        using var dir = new TempDir();
        var layout = SetupGitContent(dir);
        // Seul main poussé (pas de branche feature) : l'exo git n'est pas « tenté ».
        var sha = BuildAndPush(layout, dir, new List<GitFixtureStep>
        {
            new() { Branch = "main", Message = "init", Files = new() { ["README.md"] = "Projet\n" } },
        }, "main");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        // Rien noté : l'exo git non commencé est ignoré (pas d'« à revoir » parasite).
        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("À revoir", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.False(progress.Exercises.ContainsKey("ex-git"));
    }

    [Fact]
    public void Run_GitExercise_AttemptedButNotMerged_ARevoir()
    {
        using var dir = new TempDir();
        var layout = SetupGitContent(dir);
        // feature poussée mais NON fusionnée dans main → tenté, mais incomplet.
        var sha = BuildAndPush(layout, dir, new List<GitFixtureStep>
        {
            new() { Branch = "main", Message = "init", Files = new() { ["README.md"] = "Projet\n" } },
            new() { Branch = "feature", Base = "main" },
            new() { Branch = "feature", Message = "ajout", Files = new() { ["feature.txt"] = "salut\n" } },
        }, "main", "feature");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("À revoir", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.ARevoir, progress.Exercises["ex-git"].Status);
    }
}
