using System.IO;
using Piscine.Core.Io;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Charge le <c>manifest.yaml</c> d'un dossier d'exercice.</summary>
public static class ExerciseManifestLoader
{
    public const string FileName = "manifest.yaml";

    public static ExerciseManifest Load(string exerciseDirectory)
    {
        var path = Path.Combine(exerciseDirectory, FileName);
        return YamlLoader.Load<ExerciseManifest>(path);
    }
}
