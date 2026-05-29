using System;
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Progression de la recrue : statut par identifiant d'exercice.</summary>
public sealed class Progress
{
    public Dictionary<string, ExerciseProgress> Exercises { get; set; } = new();
}

/// <summary>Progression d'un exercice donné.</summary>
public sealed class ExerciseProgress
{
    public ExerciseStatus Status { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset? LastAttempt { get; set; }
}
