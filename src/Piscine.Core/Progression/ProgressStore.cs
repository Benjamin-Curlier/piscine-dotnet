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
        // récupérer) si le process est interrompu en plein File.WriteAllText.
        var temp = _path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _path, overwrite: true);
    }
}
