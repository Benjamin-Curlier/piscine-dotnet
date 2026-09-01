using System.IO;

namespace Piscine.Core.Content;

/// <summary>Copie les fichiers du dossier <c>starter/</c> d'un exercice vers le workspace.</summary>
public static class StarterInstaller
{
    public const string StarterDirName = "starter";

    public static void Install(string exerciseContentDir, string workspaceExerciseDir)
    {
        var workspaceRoot = Path.GetFullPath(workspaceExerciseDir);
        Directory.CreateDirectory(workspaceRoot);

        var contentRoot = Path.GetFullPath(exerciseContentDir);
        var starterDir = Path.GetFullPath(Path.Combine(contentRoot, StarterDirName));
        if (!IsWithin(contentRoot, starterDir))
        {
            throw new InvalidOperationException("Le starter doit rester dans le dossier de contenu de l'exercice.");
        }

        if (!Directory.Exists(starterDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(starterDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(starterDir, file);
            var destination = Path.GetFullPath(Path.Combine(workspaceRoot, relative));
            if (!IsWithin(workspaceRoot, destination))
            {
                throw new InvalidOperationException("Un fichier starter tente de sortir du workspace de l'exercice.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (!File.Exists(destination))
            {
                File.Copy(file, destination);
            }
        }
    }

    private static bool IsWithin(string root, string candidate)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var normalizedCandidate = Path.GetFullPath(candidate);
        var prefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        return normalizedCandidate.StartsWith(prefix, comparison);
    }
}
