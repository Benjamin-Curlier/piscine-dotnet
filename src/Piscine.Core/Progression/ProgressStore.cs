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

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<Progress>(json, Options) ?? new Progress();
    }

    public void Save(Progress progress)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(progress, Options);
        File.WriteAllText(_path, json);
    }
}
