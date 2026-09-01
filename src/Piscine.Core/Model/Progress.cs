using System;
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Progression de la recrue : statut par identifiant d'exercice.</summary>
public sealed class Progress
{
    public Dictionary<string, ExerciseProgress> Exercises { get; set; } = new();

    /// <summary>Jours où au moins une correction réelle a été lancée, pour calculer la série.</summary>
    public List<DateOnly> PracticeDays { get; set; } = new();
}

/// <summary>Progression d'un exercice donné.</summary>
public sealed class ExerciseProgress
{
    public ExerciseStatus Status { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset? LastAttempt { get; set; }

    /// <summary>
    /// Première réussite : persiste l'acquis et les XP même si une tentative ultérieure échoue.
    /// Une ancienne progression sans ce champ reste compatible via <see cref="Status"/>.
    /// </summary>
    public DateTimeOffset? FirstSucceededAt { get; set; }
}
