using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Une étape de notation déclarée dans le manifest (type io / unit / norme).</summary>
public sealed class GradingStep
{
    public string Type { get; set; } = string.Empty;

    /// <summary>Cas d'exécution pour le grader <c>io</c>.</summary>
    public List<IoCase> Cases { get; set; } = new();

    /// <summary>Fichiers de tests cachés pour le grader <c>unit</c> (consommé à l'It.3).</summary>
    public List<string> TestFiles { get; set; } = new();

    /// <summary>Pour le grader <c>norme</c> : si vrai, un écart de norme fait échouer l'exercice.</summary>
    public bool Blocking { get; set; }
}

/// <summary>Un cas d'exécution pour le grader <c>io</c>.</summary>
public sealed class IoCase
{
    public List<string> Args { get; set; } = new();

    public string Stdin { get; set; } = string.Empty;

    public string ExpectStdout { get; set; } = string.Empty;

    public int ExpectExit { get; set; }
}
