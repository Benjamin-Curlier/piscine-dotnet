using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Corrige une étape de notation à partir des sources livrées par la recrue.</summary>
public interface IGrader
{
    /// <summary>Type de l'étape gérée (ex. <c>io</c>, <c>norme</c>).</summary>
    string Type { get; }

    GraderResult Grade(IReadOnlyDictionary<string, string> sources, GradingStep step);
}
