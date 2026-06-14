using System;
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;

namespace Piscine.App.Launch;

/// <summary>
/// Ouvre l'exercice côté recrue : résout le dossier de travail, le <b>scaffolde</b> depuis le starter à
/// la 1ʳᵉ ouverture (équivalent de <c>piscine start</c>), puis le lance (dossier / éditeur / terminal
/// système) via <see cref="IProcessLauncher"/>. Le « terminal intégré » est une navigation UI (hors d'ici).
/// </summary>
public sealed class WorkspaceLauncher(PiscineLayout layout, IProcessLauncher launcher)
{
    /// <summary>Dossier de travail de l'exo, scaffoldé si vide/absent. null si exo introuvable.</summary>
    public string? PrepareWorkspace(string exerciseId)
    {
        var loc = ContentLocator.FindExercise(layout.Content, exerciseId);
        if (loc is null)
        {
            return null;
        }

        var dir = layout.WorkspaceExerciseDir(loc.ModuleId, exerciseId);
        if (!Directory.Exists(dir) || !Directory.EnumerateFileSystemEntries(dir).Any())
        {
            StarterInstaller.Install(loc.ContentDir, dir);
        }

        return dir;
    }

    public bool OpenFolder(string exerciseId) => Launch(exerciseId, FolderSpec);

    public bool OpenEditor(string exerciseId, EditorOption editor) =>
        Launch(exerciseId, dir => new LaunchSpec(editor.FileName, [dir]));

    public bool OpenSystemTerminal(string exerciseId) => Launch(exerciseId, TerminalSpec);

    private bool Launch(string exerciseId, Func<string, LaunchSpec> spec)
    {
        var dir = PrepareWorkspace(exerciseId);
        return dir is not null && launcher.Launch(spec(dir));
    }

    private static LaunchSpec FolderSpec(string dir) => OperatingSystem.IsWindows()
        ? new LaunchSpec("explorer.exe", [dir])
        : new LaunchSpec("xdg-open", [dir]);

    private static LaunchSpec TerminalSpec(string dir) => OperatingSystem.IsWindows()
        ? new LaunchSpec("wt.exe", ["-d", dir])
        : new LaunchSpec("x-terminal-emulator", ["--working-directory", dir]);
}
