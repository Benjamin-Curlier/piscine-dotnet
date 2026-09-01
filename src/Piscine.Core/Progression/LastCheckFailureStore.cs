using System;
using System.IO;
using System.Text.Json;

namespace Piscine.Core.Progression;

/// <summary>Persistance atomique et résiliente du dernier échec de <c>piscine check</c>.</summary>
public sealed class LastCheckFailureStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _path;

    public LastCheckFailureStore(string path) => _path = path;

    public LastCheckFailure? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LastCheckFailure>(File.ReadAllText(_path), Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Save(LastCheckFailure failure)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(failure, Options));
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Nettoyage best-effort : ne masque jamais la vraie erreur d'écriture.
        }
    }
}
