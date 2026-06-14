using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        // Énumérations (thème, cible terminal) sérialisées en chaînes camelCase → JSON lisible et
        // git-friendly (« system »/« light »/« dark »), aligné sur les valeurs lues côté JS (theme.js).
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
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

            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOpts)
                ?? new AppSettings();

            // Assainir les valeurs (l'échelle de police peut avoir été éditée à la main hors plage).
            return loaded.Normalized();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(layout.StateDir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings.Normalized(), JsonOpts));
    }
}
