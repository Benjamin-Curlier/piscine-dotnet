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
        // /resultat, watcher) ne voit jamais un JSON tronqué si l'écriture est interrompue. Nom du
        // temporaire UNIQUE par écriture (GUID) pour ne pas collisionner avec une écriture concurrente
        // (hook grade-received vs. check CLI/Desktop) sur un « .tmp » partagé.
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
