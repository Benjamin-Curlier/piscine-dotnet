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

    /// <summary>Résultat riche du dernier push (diff/indice/cours) écrit par <c>grade-received</c>.</summary>
    public string LastPushResultPath => Path.Combine(StateDir, "last-push-result.json");

    /// <summary>Dépôt bare local servant d'« origin » (le « GitLab » de la piscine).</summary>
    public string RemoteRepoPath => Path.Combine(StateDir, "remote.git");

    public string WorkspaceExerciseDir(string moduleId, string exerciseId) =>
        Path.Combine(WorkspaceRoot, moduleId, exerciseId);

    /// <summary>
    /// Résout depuis les variables d'environnement, avec des valeurs par défaut. Résolveur partagé par
    /// le CLI, le hook <c>grade-received</c> et les hôtes GUI (Desktop/DevHost) pour garantir un seul
    /// emplacement de workspace/état. <paramref name="defaultContentRoot"/> permet à un hôte de fournir
    /// son propre repli de contenu (ex. le catalogue embarqué) quand <c>PISCINE_CONTENT</c> est absent.
    /// </summary>
    public static PiscineLayout FromEnvironment(string? defaultContentRoot = null)
    {
        var content = Environment.GetEnvironmentVariable("PISCINE_CONTENT")
            ?? defaultContentRoot
            ?? Path.Combine(AppContext.BaseDirectory, "content");

        var home = Environment.GetEnvironmentVariable("PISCINE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "piscine");

        // PISCINE_WORKSPACE prime sur home/workspace (le GUI l'honore déjà ; le CLI/hook divergeaient
        // sans ça). Repli : un sous-dossier « workspace » de PISCINE_HOME.
        var workspace = Environment.GetEnvironmentVariable("PISCINE_WORKSPACE")
            ?? Path.Combine(home, "workspace");

        return new PiscineLayout(content, workspace, Path.Combine(home, ".state"));
    }
}
