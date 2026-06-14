using System;
using System.IO;
using System.Linq;
using Piscine.App.Launch;
using Piscine.Core;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>
/// Tests de <see cref="WorkspaceLauncher"/> avec le content RÉEL du dépôt (exo ex00-hello, module
/// 00-setup-git, starter/Hello.cs) + un workspace temp + un launcher enregistreur (aucun spawn réel).
/// </summary>
public sealed class WorkspaceLauncherTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string ContentRoot = Path.Combine(RepoRoot, "content");
    private const string ModuleId = "00-setup-git";
    private const string ExerciseId = "ex00-hello";

    private sealed class RecordingLauncher : IProcessLauncher
    {
        public LaunchSpec? Last { get; private set; }
        public bool Launch(LaunchSpec spec) { Last = spec; return true; }
    }

    private static (WorkspaceLauncher Launcher, RecordingLauncher Rec, string WorkspaceRoot) Create(TempDir tmp)
    {
        var workspaceRoot = Path.Combine(tmp.Path, "workspace");
        var layout = new PiscineLayout(ContentRoot, workspaceRoot, Path.Combine(tmp.Path, "state"));
        var rec = new RecordingLauncher();
        return (new WorkspaceLauncher(layout, rec), rec, workspaceRoot);
    }

    [Fact]
    public void PrepareWorkspace_scaffolds_starter_on_first_open()
    {
        using var tmp = new TempDir();
        var (launcher, _, workspaceRoot) = Create(tmp);

        var dir = launcher.PrepareWorkspace(ExerciseId);

        Assert.Equal(Path.Combine(workspaceRoot, ModuleId, ExerciseId), dir);
        Assert.True(Directory.Exists(dir));
        Assert.True(File.Exists(Path.Combine(dir!, "Hello.cs")), "Le starter Hello.cs doit être copié.");
    }

    [Fact]
    public void PrepareWorkspace_does_not_overwrite_existing_work()
    {
        using var tmp = new TempDir();
        var (launcher, _, workspaceRoot) = Create(tmp);
        var dir = Path.Combine(workspaceRoot, ModuleId, ExerciseId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Hello.cs"), "// mon travail");

        launcher.PrepareWorkspace(ExerciseId);

        Assert.Equal("// mon travail", File.ReadAllText(Path.Combine(dir, "Hello.cs")));
    }

    [Fact]
    public void OpenFolder_launches_file_manager_on_exo_dir()
    {
        using var tmp = new TempDir();
        var (launcher, rec, workspaceRoot) = Create(tmp);

        Assert.True(launcher.OpenFolder(ExerciseId));
        Assert.NotNull(rec.Last);
        var expected = OperatingSystem.IsWindows() ? "explorer.exe" : "xdg-open";
        Assert.Equal(expected, rec.Last!.FileName);
        Assert.Contains(Path.Combine(workspaceRoot, ModuleId, ExerciseId), rec.Last.Arguments);
    }

    [Fact]
    public void OpenEditor_launches_editor_command_on_exo_dir()
    {
        using var tmp = new TempDir();
        var (launcher, rec, workspaceRoot) = Create(tmp);

        Assert.True(launcher.OpenEditor(ExerciseId, new EditorOption("VS Code", "code")));
        Assert.Equal("code", rec.Last!.FileName);
        Assert.Contains(Path.Combine(workspaceRoot, ModuleId, ExerciseId), rec.Last.Arguments);
    }

    [Fact]
    public void Unknown_exercise_does_nothing()
    {
        using var tmp = new TempDir();
        var (launcher, rec, _) = Create(tmp);

        Assert.Null(launcher.PrepareWorkspace("ex-inexistant"));
        Assert.False(launcher.OpenFolder("ex-inexistant"));
        Assert.Null(rec.Last);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }
}
