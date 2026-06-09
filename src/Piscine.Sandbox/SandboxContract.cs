using System.Text.Json.Serialization;

namespace Piscine.Sandbox;

/// <summary>Requête passée au bac à sable (sérialisée dans request.json).</summary>
public sealed class SandboxRequest
{
    /// <summary>"io" ou "xunit".</summary>
    public string Mode { get; set; } = "io";

    /// <summary>Arguments programme (mode io ; pour reseau, host/port en tête).</summary>
    public string[] Args { get; set; } = System.Array.Empty<string>();

    /// <summary>Entrée standard (mode io).</summary>
    public string Stdin { get; set; } = string.Empty;

    /// <summary>Chemins d'assemblies à résoudre par l'ALC (secours ; en général redondant).</summary>
    public string[] ReferencePaths { get; set; } = System.Array.Empty<string>();
}

/// <summary>Résultat produit par le bac à sable (sérialisé dans result.json).</summary>
public sealed class SandboxResult
{
    // Mode io
    public string Stdout { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }

    // Mode xunit
    public int FactCount { get; set; }
    public string[] Failures { get; set; } = System.Array.Empty<string>();

    // L'enfant est sorti tôt via Environment.Exit (résultat partiel).
    public bool ExitedEarly { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SandboxRequest))]
[JsonSerializable(typeof(SandboxResult))]
public partial class SandboxJsonContext : JsonSerializerContext;
