using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piscine.Core.Model;

namespace Piscine.Core.Progression;

/// <summary>Persiste la progression de la recrue dans un fichier JSON.</summary>
public sealed class ProgressStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public ProgressStore(string path)
    {
        _path = path;
    }

    public Progress Load()
    {
        if (!File.Exists(_path))
        {
            return new Progress();
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Progress>(json, Options) ?? new Progress();
        }
        catch (JsonException)
        {
            // progress.json corrompu (édité à la main, ou écriture précédente interrompue) : on
            // repart d'une progression vide plutôt que de planter `check` et, surtout, le hook
            // `grade-received` (post-receive) qui sinon casserait le `git push` de la recrue.
            return new Progress();
        }
    }

    public void Save(Progress progress)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(progress, Options);

        // Écriture atomique : on écrit un fichier temporaire voisin puis on le déplace par-dessus la
        // cible, pour ne jamais laisser un progress.json à moitié écrit (que Load() devrait ensuite
        // récupérer) si le process est interrompu en plein File.WriteAllText. Le nom du temporaire est
        // UNIQUE par écriture (GUID) : le hook grade-received (post-receive) et un `check` CLI/Desktop
        // peuvent sauvegarder en parallèle sans se disputer un même « .tmp » (IOException de partage,
        // ou File.Move sur un temp déjà consommé par l'autre process).
        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, json);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            TryDeleteTemp(temp);
        }
    }

    /// <summary>Supprime le temporaire s'il subsiste (échec avant/pendant le Move). Best-effort.</summary>
    private static void TryDeleteTemp(string temp)
    {
        try
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // On ne laisse jamais l'échec du nettoyage masquer la vraie erreur (ni casser le Move réussi).
        }
    }
}
