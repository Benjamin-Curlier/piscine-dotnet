namespace Piscine.Core.Model;

/// <summary>
/// Statut de progression persisté d'un exercice.
/// (« Non corrigé » est un résultat transitoire de la moulinette, non persisté ici.)
/// </summary>
public enum ExerciseStatus
{
    NonCommence,
    ARevoir,
    Reussi
}
