using Piscine.Core.Progression;

namespace Piscine.App.Push;

/// <summary>
/// Surveille <c>last-push-result.json</c> écrit exclusivement par <c>grade-received</c> et publie un
/// événement par rendu. Lecture seule — n'écrit rien.
/// </summary>
public interface IPushResultWatcher : IAsyncDisposable
{
    /// <summary>Déclenché (thread de fond) à chaque nouveau rendu non vide.</summary>
    public event Action<PushResult>? ResultReceived;

    /// <summary>Dernier résultat reçu, ou <c>null</c> si aucun depuis le démarrage.</summary>
    public PushResult? LatestResult();

    /// <summary>
    /// Verdict <b>riche</b> du dernier push (diff/indice/cours), issu du même document corrélé que
    /// <see cref="LatestResult"/>. <c>null</c> si l'artefact est absent.
    /// </summary>
    public PushResultDocument? LatestRichResult();

    /// <summary>
    /// Démarre la surveillance (idempotent). Absorbe l'artefact existant sans le republier.
    /// </summary>
    public void Start();
}
