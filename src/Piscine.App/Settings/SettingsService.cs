using System;
using System.IO;
using System.Text.Json;
using Piscine.Core;

namespace Piscine.App.Settings;

/// <summary>
/// Lit/écrit les réglages dans <c>settings.json</c> (répertoire d'état). Fail-soft : renvoie les
/// réglages par défaut si le fichier est absent ou corrompu (jamais d'exception au chargement).
/// </summary>
public sealed class SettingsService(PiscineLayout layout)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private string FilePath => Path.Combine(layout.StateDir, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOpts) ?? new AppSettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(layout.StateDir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOpts));
    }
}
