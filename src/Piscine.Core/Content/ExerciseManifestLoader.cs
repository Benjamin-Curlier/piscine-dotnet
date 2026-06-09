using System.IO;
using Piscine.Core.Io;
using Piscine.Core.Model;
using YamlDotNet.Core;

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

    /// <summary>
    /// Re-parse le manifest en mode STRICT (toute clé inconnue = erreur) et renvoie un message décrivant
    /// la première clé non reconnue, ou <c>null</c> si le manifest est strictement valide. Le chargement
    /// runtime (<see cref="Load"/>) reste tolérant ; cette passe sert au gate <c>validate-content</c> à
    /// détecter les clés mal orthographiées (ex. <c>expext_stdout</c>) qui seraient sinon silencieusement
    /// ignorées → mis-grade. Suppose un manifest déjà chargé sans erreur en mode lenient (sinon le seul
    /// écart strict possible est une clé inconnue).
    /// </summary>
    public static string? FindUnknownKey(string exerciseDirectory)
    {
        var path = Path.Combine(exerciseDirectory, FileName);
        try
        {
            YamlLoader.DeserializeStrict<ExerciseManifest>(File.ReadAllText(path));
            return null;
        }
        catch (YamlException e)
        {
            // InnerException porte le message net (« Property 'X' not found on type '...' ») ; e.Start.Line
            // localise la clé fautive.
            var detail = e.InnerException?.Message ?? e.Message;
            return e.Start.Line > 0 ? $"{FileName} (ligne {e.Start.Line}) : {detail}" : $"{FileName} : {detail}";
        }
    }
}
