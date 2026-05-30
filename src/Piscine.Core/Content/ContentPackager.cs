using System;
using System.IO;

namespace Piscine.Core.Content;

/// <summary>
/// Copie une arborescence de contenu vers une destination en EXCLUANT tout dossier
/// <c>solution/</c> (les corrigés de référence ne sont jamais distribués). (spec §3.3)
/// </summary>
public static class ContentPackager
{
    public const string SolutionDirName = "solution";

    public static void CopyWithoutSolutions(string sourceContentDir, string destContentDir)
    {
        foreach (var file in Directory.EnumerateFiles(sourceContentDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceContentDir, file);
            if (HasSolutionSegment(relative))
            {
                continue;
            }

            var destination = Path.Combine(destContentDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static bool HasSolutionSegment(string relativePath)
    {
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Equals(SolutionDirName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
