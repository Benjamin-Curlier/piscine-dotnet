using System;
using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Applique des résultats de correction à un <see cref="Progress"/> persistable.</summary>
public static class ProgressRecorder
{
    public static void Apply(Progress progress, IEnumerable<ExerciseGradingResult> results, DateTimeOffset now)
    {
        foreach (var result in results)
        {
            if (result.Status == GraderStatus.NonCorrige)
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
            entry.Status = result.Status == GraderStatus.Reussi
                ? ExerciseStatus.Reussi
                : ExerciseStatus.ARevoir;
        }
    }
}
