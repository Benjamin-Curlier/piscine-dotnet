using System;
using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Applique des résultats de correction à un <see cref="Progress"/> persistable.</summary>
public static class ProgressRecorder
{
    public static void Apply(Progress progress, IEnumerable<ExerciseGradingResult> results, DateTimeOffset now)
    {
        var practiced = false;
        foreach (var result in results)
        {
            if (result.Status == GraderStatus.NonCorrige)
            {
                continue;
            }

            // Panne d'infrastructure (bac à sable indisponible) : on n'écrit RIEN — ne pas rétrograder
            // un « Réussi » ni compter une tentative pour un incident qui n'est pas le fait de la recrue (M-10).
            if (result.IsInternalError)
            {
                continue;
            }

            if (!progress.Exercises.TryGetValue(result.ExerciseId, out var entry))
            {
                entry = new ExerciseProgress();
                progress.Exercises[result.ExerciseId] = entry;
            }

            entry.Attempts++;
            entry.LastAttempt = now;
            practiced = true;
            entry.Status = result.Status == GraderStatus.Reussi
                ? ExerciseStatus.Reussi
                : ExerciseStatus.ARevoir;

            if (result.Status == GraderStatus.Reussi)
            {
                entry.FirstSucceededAt ??= now;
            }
        }

        if (practiced)
        {
            var day = DateOnly.FromDateTime(now.LocalDateTime);
            if (!progress.PracticeDays.Contains(day))
            {
                progress.PracticeDays.Add(day);
                progress.PracticeDays.Sort();
            }
        }
    }
}
