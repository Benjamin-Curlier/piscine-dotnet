namespace Piscine.Git;

/// <summary>Génère le hook <c>post-receive</c> qui déclenche la moulinette sur la branche de rendu.</summary>
public static class HookScript
{
    /// <summary>
    /// Script <c>post-receive</c> : pour la seule référence officielle <c>refs/heads/main</c>, appelle
    /// <c>piscine grade-received &lt;oldrev&gt; &lt;newrev&gt;</c>. Le chemin de l'exécutable est
    /// normalisé en slashes pour <c>sh</c> (MinGit sous Windows). Lignes en LF.
    /// </summary>
    public static string PostReceive(string piscineExecutablePath)
    {
        var exe = piscineExecutablePath.Replace('\\', '/');
        return string.Join('\n',
            "#!/bin/sh",
            "while read oldrev newrev refname; do",
            "  if [ \"$refname\" = \"refs/heads/main\" ]; then",
            $"    \"{exe}\" grade-received \"$oldrev\" \"$newrev\"",
            "  fi",
            "done",
            "");
    }
}
