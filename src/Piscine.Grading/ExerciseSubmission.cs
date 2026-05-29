using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Un exercice prêt à corriger : son manifest et le contexte de correction.</summary>
public sealed class ExerciseSubmission
{
    public ExerciseSubmission(ExerciseManifest manifest, GradingContext context)
    {
        Manifest = manifest;
        Context = context;
    }

    public ExerciseManifest Manifest { get; }

    public GradingContext Context { get; }
}
