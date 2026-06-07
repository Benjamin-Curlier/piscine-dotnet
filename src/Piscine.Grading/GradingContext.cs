using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>Entrées d'une correction : sources livrées par la recrue + fichiers grader cachés.</summary>
public sealed class GradingContext
{
    public GradingContext(
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyDictionary<string, string>? graderFiles = null,
        string? repositoryPath = null,
        string? headRef = null)
    {
        Sources = sources;
        GraderFiles = graderFiles ?? new Dictionary<string, string>();
        RepositoryPath = repositoryPath;
        HeadRef = headRef;
    }

    /// <summary>Fichiers livrés par la recrue (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>Fichiers de notation cachés, ex. tests xUnit (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> GraderFiles { get; }

    /// <summary>Chemin du dépôt git rendu à inspecter (grader <c>git</c>), ou <c>null</c>.</summary>
    public string? RepositoryPath { get; }

    /// <summary>
    /// Branche à traiter comme <c>HEAD</c> par le grader <c>git</c> quand le dépôt inspecté n'a pas son
    /// HEAD sur la branche de rendu — cas du **dépôt bare** côté <c>grade-received</c> (après un push,
    /// le bare a la ref <c>main</c> mais son HEAD reste orphelin). <c>null</c> ⇒ le grader lit
    /// <c>repo.Head</c> (check local + fixture <c>validate-content</c>, inchangés).
    /// </summary>
    public string? HeadRef { get; }
}
