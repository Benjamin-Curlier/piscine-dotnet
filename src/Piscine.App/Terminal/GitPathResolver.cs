namespace Piscine.App.Terminal;

/// <summary>
/// Resout le vrai <c>git</c> sur un PATH donne en EXCLUANT le dossier du shim (pour ne jamais
/// re-executer le shim). Pur et testable : aucune lecture d'environnement implicite.
/// </summary>
public static class GitPathResolver
{
    /// <summary>
    /// Renvoie le chemin absolu du premier <c>git</c>/<c>git.exe</c> trouve sur <paramref name="path"/>
    /// dont le dossier n'est PAS <paramref name="excludeDir"/> (comparaison plein-chemin, insensible a
    /// la casse), ou <c>null</c> si aucun.
    /// </summary>
    public static string? Resolve(string path, string? excludeDir)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var normalizedExclude = NormalizeDir(excludeDir);
        var candidates = OperatingSystem.IsWindows()
            ? new[] { "git.exe", "git.cmd", "git" }
            : new[] { "git" };

        foreach (var dir in path.Split(System.IO.Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            var normalizedDir = NormalizeDir(dir);
            if (normalizedDir is null)
            {
                continue;
            }

            if (normalizedExclude is not null
                && string.Equals(normalizedDir, normalizedExclude, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var name in candidates)
            {
                var candidate = System.IO.Path.Combine(normalizedDir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string? NormalizeDir(string? dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            return null;
        }

        try
        {
            return System.IO.Path.GetFullPath(dir).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
