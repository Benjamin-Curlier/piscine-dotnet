using System;
using System.IO;

namespace Piscine.Core;

/// <summary>Résout les emplacements de la piscine : contenu, workspace, état persistant.</summary>
public sealed class PiscineLayout
{
    public PiscineLayout(string contentRoot, string workspaceRoot, string stateDir)
    {
        ContentRoot = contentRoot;
        WorkspaceRoot = workspaceRoot;
        StateDir = stateDir;
    }

    public string ContentRoot { get; }

    public PiscinePaths Content => new(ContentRoot);

    public string WorkspaceRoot { get; }

    public string StateDir { get; }

    public string ProgressPath => Path.Combine(StateDir, "progress.json");

    public string WorkspaceExerciseDir(string moduleId, string exerciseId) =>
        Path.Combine(WorkspaceRoot, moduleId, exerciseId);

    /// <summary>Résout depuis les variables d'environnement, avec des valeurs par défaut.</summary>
    public static PiscineLayout FromEnvironment()
    {
        var content = Environment.GetEnvironmentVariable("PISCINE_CONTENT")
            ?? Path.Combine(AppContext.BaseDirectory, "content");

        var home = Environment.GetEnvironmentVariable("PISCINE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "piscine");

        return new PiscineLayout(content, Path.Combine(home, "workspace"), Path.Combine(home, ".state"));
    }
}
