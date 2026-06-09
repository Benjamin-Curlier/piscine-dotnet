using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Piscine.Core.Io;

/// <summary>Désérialise des fichiers YAML de contenu (clés en underscore → propriétés PascalCase).</summary>
public static class YamlLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    // Variante STRICTE (sans IgnoreUnmatchedProperties) : lève sur toute clé inconnue. Réservée au
    // gate validate-content (détection des clés mal orthographiées) ; le chargement runtime reste lenient.
    private static readonly IDeserializer StrictDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public static T Load<T>(string path)
    {
        var yaml = File.ReadAllText(path);
        return Deserialize<T>(yaml);
    }

    public static T Deserialize<T>(string yaml) => Deserializer.Deserialize<T>(yaml);

    /// <summary>Désérialise en mode strict : toute clé non mappée lève une <c>YamlException</c>.</summary>
    public static T DeserializeStrict<T>(string yaml) => StrictDeserializer.Deserialize<T>(yaml);
}
