using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Corrige une étape de notation à partir du contexte de correction.</summary>
public interface IGrader
{
    /// <summary>Type de l'étape gérée (ex. <c>io</c>, <c>norme</c>, <c>unit</c>).</summary>
    string Type { get; }

    GraderResult Grade(GradingContext context, GradingStep step);
}
