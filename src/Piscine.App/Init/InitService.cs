using System.IO;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Git;

namespace Piscine.App.Init;

/// <summary>
/// Wrapper mince sur <see cref="GitWorkspace.Initialize"/> : expose l'état courant
/// (<see cref="Status"/>) et l'action d'initialisation (<see cref="Initialize"/>)
/// avec un rapport avant/après. Ne duplique aucune logique git.
/// </summary>
public sealed class InitService(PiscineLayout layout, string piscineExecutablePath)
{
    private readonly PiscineLayout _layout = layout;
    private readonly string _exe = piscineExecutablePath;

    /// <summary>
    /// Inspecte l'état de l'environnement git sans rien écrire.
    /// </summary>
    public InitStatus Status()
    {
        var workspaceReady = Repository.IsValid(_layout.WorkspaceRoot);
        var bareRepoReady = Repository.IsValid(_layout.RemoteRepoPath);
        var hookInstalled = File.Exists(Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive"));

        bool originConfigured = false;
        if (workspaceReady)
        {
            using var repo = new Repository(_layout.WorkspaceRoot);
            originConfigured = repo.Network.Remotes[GitWorkspace.OriginName] is not null;
        }

        return new InitStatus(workspaceReady, bareRepoReady, hookInstalled, originConfigured);
    }

    /// <summary>
    /// Initialise l'environnement en appelant <see cref="GitWorkspace.Initialize"/>.
    /// L'opération est idempotente (garantie par le moteur). Retourne un <see cref="InitOutcome"/>
    /// avec l'état avant/après et un message lisible.
    /// </summary>
    public InitOutcome Initialize()
    {
        var before = Status();

        try
        {
            GitWorkspace.Initialize(_layout, _exe);
        }
        catch (Exception ex) when (ex is LibGit2SharpException or IOException or UnauthorizedAccessException)
        {
            return new InitOutcome(false, before, before, "Échec de l'initialisation.", ex.Message);
        }

        var after = Status();
        var message = before.IsInitialized ? "Déjà initialisé." : "Environnement initialisé.";
        return new InitOutcome(after.IsInitialized, before, after, message, null);
    }
}
