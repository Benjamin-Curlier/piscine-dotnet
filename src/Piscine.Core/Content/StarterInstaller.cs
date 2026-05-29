using System.IO;

namespace Piscine.Core.Content;

/// <summary>Copie les fichiers du dossier <c>starter/</c> d'un exercice vers le workspace.</summary>
public static class StarterInstaller
{
    public const string StarterDirName = "starter";

    public static void Install(string exerciseContentDir, string workspaceExerciseDir)
    {
        Directory.CreateDirectory(workspaceExerciseDir);

        var starterDir = Path.Combine(exerciseContentDir, StarterDirName);
        if (!Directory.Exists(starterDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(starterDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(starterDir, file);
            var destination = Path.Combine(workspaceExerciseDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (!File.Exists(destination))
            {
                File.Copy(file, destination);
            }
        }
    }
}
