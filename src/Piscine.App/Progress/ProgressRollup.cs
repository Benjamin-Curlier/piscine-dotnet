using System.Collections.Generic;
using System.Linq;

namespace Piscine.App.Progress;

/// <summary>
/// Statut représentatif d'un module à partir des statuts de ses exercices. Pur et déterministe.
/// Priorité : un exo à revoir prime ; sinon « tous poussés/notés » = complet ; sinon tout travail
/// entamé = en cours ; sinon non commencé.
/// </summary>
public static class ProgressRollup
{
    public static ExerciseProgressStatus ForModule(IReadOnlyList<ExerciseProgressStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return ExerciseProgressStatus.NonCommence;
        }

        if (statuses.Any(s => s == ExerciseProgressStatus.ARevoir))
        {
            return ExerciseProgressStatus.ARevoir;
        }

        if (statuses.All(s => s == ExerciseProgressStatus.PousseNote))
        {
            return ExerciseProgressStatus.PousseNote;
        }

        var anyStarted = statuses.Any(s =>
            s is ExerciseProgressStatus.EnCours
              or ExerciseProgressStatus.CommiteNonPousse
              or ExerciseProgressStatus.PousseNote);

        return anyStarted ? ExerciseProgressStatus.EnCours : ExerciseProgressStatus.NonCommence;
    }
}
