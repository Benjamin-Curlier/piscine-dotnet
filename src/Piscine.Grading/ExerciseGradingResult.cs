using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat agrégé de la correction d'un exercice.</summary>
public sealed class ExerciseGradingResult
{
    public ExerciseGradingResult(string exerciseId, IEnumerable<GraderResult> results)
        : this(exerciseId, Aggregate(results, out var list), list)
    {
    }

    private ExerciseGradingResult(string exerciseId, GraderStatus status, IReadOnlyList<GraderResult> results)
    {
        ExerciseId = exerciseId;
        Status = status;
        Results = results;
    }

    public string ExerciseId { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<GraderResult> Results { get; }

    /// <summary>Exercice non corrigé (un exercice précédent du groupe est à revoir).</summary>
    public static ExerciseGradingResult NotGraded(string exerciseId) =>
        new(exerciseId, GraderStatus.NonCorrige, new List<GraderResult>());

    private static GraderStatus Aggregate(IEnumerable<GraderResult> results, out IReadOnlyList<GraderResult> list)
    {
        list = results.ToList();
        return list.Any(r => r.Status == GraderStatus.ARevoir)
            ? GraderStatus.ARevoir
            : GraderStatus.Reussi;
    }
}
