using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>
/// Un exercice désérialisé depuis <c>manifest.yaml</c>.
/// La section <c>constraints</c> sera ajoutée quand elle sera appliquée.
/// </summary>
public sealed class ExerciseManifest
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public List<string> Deliverables { get; set; } = new();

    public List<string> Starter { get; set; } = new();

    public List<GradingStep> Grading { get; set; } = new();

    public FeedbackConfig Feedback { get; set; } = new();
}
