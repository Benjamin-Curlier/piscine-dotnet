using System.IO;

namespace Piscine.Core.Content;

/// <summary>Retrouve un exercice par identifiant en scannant les modules.</summary>
public static class ContentLocator
{
    public const string ExercisesDirName = "exercises";

    public static ExerciseLocation? FindExercise(PiscinePaths content, string exerciseId)
    {
        if (!Directory.Exists(content.ModulesDirectory))
        {
            return null;
        }

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

        return null;
    }
}
