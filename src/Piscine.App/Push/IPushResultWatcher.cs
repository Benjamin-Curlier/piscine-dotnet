namespace Piscine.App.Push;

/// <summary>
/// Surveille <c>progress.json</c> écrit par <c>grade-received</c> et publie un événement
/// à chaque changement réel (delta vs snapshot initial). Lecture seule — n'écrit rien.
/// </summary>
public interface IPushResultWatcher : IAsyncDisposable
{
    /// <summary>Déclenché (thread de fond) à chaque nouveau delta non vide.</summary>
    event Action<PushResult>? ResultReceived;

    /// <summary>Dernier résultat reçu, ou <c>null</c> si aucun depuis le démarrage.</summary>
    PushResult? LatestResult();

    /// <summary>
    /// Démarre la surveillance (idempotent). Prend un snapshot initial de <c>progress.json</c>
    /// pour ne publier que les delta suivants.
    /// </summary>
    void Start();
}
