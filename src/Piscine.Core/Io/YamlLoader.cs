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

    public static T Load<T>(string path)
    {
        var yaml = File.ReadAllText(path);
        return Deserialize<T>(yaml);
    }

    public static T Deserialize<T>(string yaml) => Deserializer.Deserialize<T>(yaml);
}
