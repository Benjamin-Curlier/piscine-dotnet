using System.IO;

namespace Piscine.Core.Content;

/// <summary>Retrouve un exercice par identifiant en scannant les modules.</summary>
public static class ContentLocator
{
    public const string ExercisesDirName = "exercises";

    /// <summary>Pseudo-module sous lequel les Rushes sont rangés (workspace, snapshot).</summary>
    public const string RushesModuleId = "rushes";

    public static ExerciseLocation? FindExercise(PiscinePaths content, string exerciseId)
    {
        if (Directory.Exists(content.ModulesDirectory))
        {
            foreach (var moduleDir in Directory.EnumerateDirectories(content.ModulesDirectory))
            {
                var exerciseDir = Path.Combine(moduleDir, ExercisesDirName, exerciseId);
                var manifestPath = Path.Combine(exerciseDir, ExerciseManifestLoader.FileName);
                var moduleManifest = Path.Combine(moduleDir, ModuleLoader.FileName);

                if (File.Exists(manifestPath) && File.Exists(moduleManifest))
                {
                    var module = ModuleLoader.Load(moduleDir);
                    return new ExerciseLocation(module.Id, exerciseId, exerciseDir);
                }
            }
        }

        // Un Rush est un livrable autonome : son dossier porte directement le manifest.
        var rushDir = Path.Combine(content.RushesDirectory, exerciseId);
        if (File.Exists(Path.Combine(rushDir, ExerciseManifestLoader.FileName)))
        {
            return new ExerciseLocation(RushesModuleId, exerciseId, rushDir);
        }

        return null;
    }
}
