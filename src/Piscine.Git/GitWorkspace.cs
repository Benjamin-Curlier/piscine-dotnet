using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp;
using Piscine.Core;

namespace Piscine.Git;

/// <summary>
/// Met en place l'environnement git de la recrue : un dépôt de travail (workspace),
/// un dépôt bare local servant d'« origin », et le hook <c>post-receive</c> qui
/// déclenche la moulinette à chaque push.
/// </summary>
public static class GitWorkspace
{
    public const string OriginName = "origin";

    public static void Initialize(PiscineLayout layout, string piscineExecutablePath)
    {
        Directory.CreateDirectory(layout.WorkspaceRoot);
        if (!Repository.IsValid(layout.WorkspaceRoot))
        {
            Repository.Init(layout.WorkspaceRoot);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(layout.RemoteRepoPath)!);
        if (!Repository.IsValid(layout.RemoteRepoPath))
        {
            Repository.Init(layout.RemoteRepoPath, isBare: true);
        }

        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            if (repo.Network.Remotes[OriginName] is null)
            {
                repo.Network.Remotes.Add(OriginName, layout.RemoteRepoPath);
            }
        }

        InstallHook(layout.RemoteRepoPath, piscineExecutablePath);
    }

    private static void InstallHook(string bareRepoPath, string piscineExecutablePath)
    {
        var hooksDir = Path.Combine(bareRepoPath, "hooks");
        Directory.CreateDirectory(hooksDir);
        var hookPath = Path.Combine(hooksDir, "post-receive");

        File.WriteAllText(hookPath, HookScript.PostReceive(piscineExecutablePath));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(
                hookPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }
}
