using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>
/// Sous-ensemble structurel d'un exercice, désérialisé depuis <c>manifest.yaml</c>.
/// Les sections grading/feedback/constraints sont ajoutées à l'It.2.
/// </summary>
public sealed class ExerciseManifest
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public List<string> Deliverables { get; set; } = new();

    public List<string> Starter { get; set; } = new();
}
