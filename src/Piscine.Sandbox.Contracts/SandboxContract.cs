using System.Text.Json.Serialization;

namespace Piscine.Sandbox;

/// <summary>
/// Constantes du protocole IPC entre le bac à sable (enfant NON fiable) et le client de lancement
/// (parent de confiance dans Piscine.Grading).
/// </summary>
public static class SandboxProtocol
{
    /// <summary>
    /// Préfixe de la trame verdict émise par l'enfant sur stdout, au format « SENTINELLE + {json} +
    /// saut de ligne ». Le parent de confiance dérive le résultat autoritaire de CETTE trame, jamais
    /// d'un fichier que la recrue pourrait écrire. Le jeton est volontairement distinctif : une
    /// collision avec une sortie recrue légitime (seul le mode xunit peut écrire sur le stdout brut)
    /// est invraisemblable, et le cas échéant elle échoue en fermeture (ArrêtAnormal), jamais vers un
    /// faux succès.
    /// </summary>
    public const string VerdictSentinel = "<<:PISCINE-SANDBOX-VERDICT-9f3a2b:>>";
}

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

/// <summary>Résultat produit par le bac à sable, transmis au parent via une trame stdout.</summary>
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

    // L'enfant est sorti tôt via Environment.Exit (résultat partiel) : le parent y recolle le code
    // de sortie réel du processus.
    public bool ExitedEarly { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SandboxRequest))]
[JsonSerializable(typeof(SandboxResult))]
public partial class SandboxJsonContext : JsonSerializerContext;
