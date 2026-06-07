using Piscine.Core.Progression;

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
    /// Verdict <b>riche</b> du dernier push (diff/indice/cours), lu à la demande depuis
    /// <c>last-push-result.json</c> (#40). <c>null</c> si l'artefact est absent (rétro-compat :
    /// la page retombe alors sur le statut seul).
    /// </summary>
    PushResultDocument? LatestRichResult();

    /// <summary>
    /// Démarre la surveillance (idempotent). Prend un snapshot initial de <c>progress.json</c>
    /// pour ne publier que les delta suivants.
    /// </summary>
    void Start();
}
