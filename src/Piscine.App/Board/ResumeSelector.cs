using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Progress;

namespace Piscine.App.Board;

/// <summary>
/// Choisit l'exercice à « Reprendre » : 1er exo actionnable dans l'ordre du curriculum. Priorité
/// décroissante : ARevoir, puis travail en cours (EnCours/CommiteNonPousse), puis NonCommence.
/// Renvoie null si tous les exos sont PousseNote (rien à reprendre). Pur.
/// </summary>
public static class ResumeSelector
{
    public static (string ModuleId, string ExerciseId)? Pick(
        IReadOnlyList<(string ModuleId, string ExerciseId)> orderedExercises,
        IReadOnlyDictionary<(string ModuleId, string ExerciseId), ExerciseProgressStatus> statuses)
    {
        static ExerciseProgressStatus Status(
            (string, string) key,
            IReadOnlyDictionary<(string, string), ExerciseProgressStatus> s)
            => s.TryGetValue(key, out var v) ? v : ExerciseProgressStatus.NonCommence;

        (string, string)? First(Func<ExerciseProgressStatus, bool> match)
            => orderedExercises.Cast<(string, string)?>()
                .FirstOrDefault(e => match(Status(e!.Value, statuses)));

        return First(s => s == ExerciseProgressStatus.ARevoir)
            ?? First(s => s is ExerciseProgressStatus.EnCours or ExerciseProgressStatus.CommiteNonPousse)
            ?? First(s => s == ExerciseProgressStatus.NonCommence);
    }
}
