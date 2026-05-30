namespace Piscine.Git;

/// <summary>Génère le hook <c>post-receive</c> qui déclenche la moulinette à chaque push.</summary>
public static class HookScript
{
    /// <summary>
    /// Script <c>post-receive</c> : pour chaque référence reçue, appelle
    /// <c>piscine grade-received &lt;newrev&gt;</c>. Le chemin de l'exécutable est
    /// normalisé en slashes pour <c>sh</c> (MinGit sous Windows). Lignes en LF.
    /// </summary>
    public static string PostReceive(string piscineExecutablePath)
    {
        var exe = piscineExecutablePath.Replace('\\', '/');
        return string.Join('\n',
            "#!/bin/sh",
            "while read oldrev newrev refname; do",
            $"  \"{exe}\" grade-received \"$newrev\"",
            "done",
            "");
    }
}
