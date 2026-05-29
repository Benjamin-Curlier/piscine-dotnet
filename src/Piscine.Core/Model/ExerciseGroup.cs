using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Un groupe d'exercices ordonné (l'ordre = correction séquentielle, stop au 1er KO).</summary>
public sealed class ExerciseGroup
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<string> Exercises { get; set; } = new();
}
