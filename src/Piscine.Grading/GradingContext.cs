using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>Entrées d'une correction : sources livrées par la recrue + fichiers grader cachés.</summary>
public sealed class GradingContext
{
    public GradingContext(
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyDictionary<string, string>? graderFiles = null,
        string? repositoryPath = null)
    {
        Sources = sources;
        GraderFiles = graderFiles ?? new Dictionary<string, string>();
        RepositoryPath = repositoryPath;
    }

    /// <summary>Fichiers livrés par la recrue (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>Fichiers de notation cachés, ex. tests xUnit (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> GraderFiles { get; }

    /// <summary>Chemin du dépôt git rendu à inspecter (grader <c>git</c>), ou <c>null</c>.</summary>
    public string? RepositoryPath { get; }
}
