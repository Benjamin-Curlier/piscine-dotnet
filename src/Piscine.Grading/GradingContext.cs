using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>Entrées d'une correction : sources livrées par la recrue + fichiers grader cachés.</summary>
public sealed class GradingContext
{
    public GradingContext(
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyDictionary<string, string>? graderFiles = null)
    {
        Sources = sources;
        GraderFiles = graderFiles ?? new Dictionary<string, string>();
    }

    /// <summary>Fichiers livrés par la recrue (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>Fichiers de notation cachés, ex. tests xUnit (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> GraderFiles { get; }
}
