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

    /// <summary>
    /// Soumission vide : l'exercice attend des livrables mais aucun n'a été trouvé sur le disque.
    /// Inutile (et trompeur) de tenter la compilation dans ce cas.
    /// </summary>
    public bool IsEmpty => Manifest.Deliverables.Count > 0 && Context.Sources.Count == 0;
}
