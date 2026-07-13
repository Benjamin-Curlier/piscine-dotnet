using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Piscine.Core.Progression;

/// <summary>
/// Persiste le résultat riche du dernier push (<c>last-push-result.json</c>) écrit par
/// <c>grade-received</c> et lu par la page <c>/resultat</c>. Absence du fichier ⇒ <c>null</c>
/// (rétro-compatible avec le comportement statut-only antérieur à #40).
/// </summary>
public sealed class LastPushResultStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public LastPushResultStore(string path)
    {
        _path = path;
    }

    /// <summary>Charge le document, ou <c>null</c> si absent ou illisible.</summary>
    public PushResultDocument? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<PushResultDocument>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(PushResultDocument document)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, Options);
        // Écriture atomique (comme ProgressStore) : écrire dans un .tmp puis Move — un lecteur (page
        // /resultat, watcher) ne voit jamais un JSON tronqué si l'écriture est interrompue.
        var temp = _path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, _path, overwrite: true);
    }
}
