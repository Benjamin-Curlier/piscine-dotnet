using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat agrégé de la correction d'un exercice.</summary>
public sealed class ExerciseGradingResult
{
    public ExerciseGradingResult(string exerciseId, IEnumerable<GraderResult> results)
    {
        ExerciseId = exerciseId;
        Results = results.ToList();
        Status = Results.Any(r => r.Status == GraderStatus.ARevoir)
            ? GraderStatus.ARevoir
            : GraderStatus.Reussi;
    }

    public string ExerciseId { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<GraderResult> Results { get; }
}
