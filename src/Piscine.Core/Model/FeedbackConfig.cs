using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Configuration de feedback éducatif d'un exercice.</summary>
public sealed class FeedbackConfig
{
    /// <summary>Ancre vers la section de cours pertinente, ex. <c>cours.md#hello-world</c>.</summary>
    public string CourseRef { get; set; } = string.Empty;

    public List<FeedbackHint> Hints { get; set; } = new();
}

/// <summary>Un indice conditionnel affiché selon un déclencheur.</summary>
public sealed class FeedbackHint
{
    /// <summary>Déclencheur, ex. <c>io_mismatch</c>.</summary>
    public string When { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
