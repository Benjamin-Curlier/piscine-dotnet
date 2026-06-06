namespace Piscine.App.Push;

/// <summary>Verdict binaire d'un exercice après push (mappé depuis <c>ExerciseStatus</c>).</summary>
public enum PushVerdict
{
    Reussi,
    ARevoir,
}

/// <summary>Résultat de statut d'un exercice ayant changé depuis le dernier snapshot.</summary>
public record PushResultEntry(
    string ExerciseId,
    PushVerdict Verdict,
    int Attempts,
    DateTimeOffset? LastAttempt);

/// <summary>Ensemble des exercices ayant changé lors d'un rendu <c>grade-received</c>.</summary>
public record PushResult(
    IReadOnlyList<PushResultEntry> Changed,
    DateTimeOffset ObservedAt);

/// <summary>Phase de la page <c>/resultat</c> après un push.</summary>
public enum PushPhase
{
    Idle,

    /// <summary>Réservé : un push détecté en cours, avant l'arrivée du résultat (sera câblé sur
    /// l'événement `push` du shim git S3 quand le terminal embarqué pilotera le rendu).</summary>
    EnAttente,

    Recu,
}
