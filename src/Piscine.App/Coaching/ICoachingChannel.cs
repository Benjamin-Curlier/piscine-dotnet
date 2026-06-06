namespace Piscine.App.Coaching;

/// <summary>
/// Canal local recevant les evenements emis par le shim <c>git</c>. Le parent (App/DevHost) possede
/// le cycle de vie ; le transport (named pipe, repli TCP) est interchangeable derriere cette interface.
/// </summary>
public interface ICoachingChannel : IAsyncDisposable
{
    /// <summary>Nom/adresse du canal a passer au shim (variable d'env <c>PISCINE_COACH_PIPE</c>).</summary>
    string Endpoint { get; }

    /// <summary>Declenche pour chaque evenement de commande git recu.</summary>
    event Action<GitCommandEvent>? CommandReceived;

    /// <summary>Demarre la boucle d'ecoute (non bloquante).</summary>
    void Start();
}
