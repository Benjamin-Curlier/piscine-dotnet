namespace Piscine.App.Coaching;

/// <summary>
/// Attentes propres a l'exercice courant, fournies par la couche UI/exercice au moteur de coaching.
/// Modele pur (immuable). Tous les champs sont optionnels : le coaching degrade proprement sans eux.
/// </summary>
public sealed record ExerciseExpectation
{
    /// <summary>Branche attendue pour l'exercice, ou <c>null</c> si indifferent.</summary>
    public string? ExpectedBranch { get; init; }

    /// <summary>
    /// La derniere correction <c>grade-received</c> a-t-elle signale un echec ?
    /// <c>null</c> si aucune correction connue.
    /// </summary>
    public bool? GradeReceivedFailed { get; init; }
}
