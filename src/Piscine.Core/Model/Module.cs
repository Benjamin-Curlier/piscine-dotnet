using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Un module pédagogique, désérialisé depuis <c>module.yaml</c>.</summary>
public sealed class Module
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>Modules à avoir parcourus avant celui-ci (conseil pédagogique, pas verrou bloquant).</summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>Acte du fil rouge auquel appartient le module.</summary>
    public string Arc { get; set; } = string.Empty;

    /// <summary>Mission narrative qui donne un but concret aux apprentissages du module.</summary>
    public string Mission { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public List<ExerciseGroup> Groups { get; set; } = new();
}
