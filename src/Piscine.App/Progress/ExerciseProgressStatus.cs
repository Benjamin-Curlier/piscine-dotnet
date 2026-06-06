namespace Piscine.App.Progress;

/// <summary>
/// Statut de progression dérivé d'un exercice, combinant <c>progress.json</c>, l'état git
/// repo-wide et la présence de fichiers dans le workspace. Best-effort : 5 valeurs UI sur
/// 3 valeurs persistées.
/// </summary>
public enum ExerciseProgressStatus
{
    NonCommence,
    EnCours,
    CommiteNonPousse,
    PousseNote,
    ARevoir,
}

/// <summary>Source du signal ayant produit le statut dérivé.</summary>
public enum StatusSource
{
    /// <summary>Statut lu directement depuis <c>progress.json</c>.</summary>
    Progress,

    /// <summary>Statut inféré de l'état git (<c>RepoState</c>) — best-effort.</summary>
    GitDerived,
}
