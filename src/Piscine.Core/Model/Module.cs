using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Un module pédagogique, désérialisé depuis <c>module.yaml</c>.</summary>
public sealed class Module
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int Order { get; set; }

    public string Course { get; set; } = string.Empty;

    public List<ExerciseGroup> Groups { get; set; } = new();
}
