using System;
using System.IO;
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
}
