using System.Collections.Generic;
using System.Linq;

namespace Piscine.App.Git;

/// <summary>
/// Photographie immuable de l'etat d'un depot git, lue sans cache par <see cref="GitStatusService"/>.
/// Modele pur (aucune dependance LibGit2Sharp exposee) consomme par le <c>CoachingService</c> et
/// par le panneau de statut. Construit a la main dans les tests de regles (pas de depot reel requis).
/// </summary>
public sealed record RepoState
{
    /// <summary>Le dossier est-il un depot git valide.</summary>
    public bool IsRepository { get; init; }

    /// <summary>Nom convivial de la branche courante, ou <c>null</c> si HEAD detache / depot sans commit.</summary>
    public string? CurrentBranch { get; init; }

    /// <summary>HEAD pointe sur un commit nu (pas sur une branche).</summary>
    public bool IsDetachedHead { get; init; }

    /// <summary>Le depot possede au moins un commit (HEAD a une pointe).</summary>
    public bool HasAnyCommit { get; init; }

    /// <summary>Nombre d'entrees indexees (stagees) pretes a committer.</summary>
    public int StagedCount { get; init; }

    /// <summary>Nombre de modifications suivies non indexees (modifiees / supprimees).</summary>
    public int UnstagedCount { get; init; }

    /// <summary>Nombre de fichiers non suivis.</summary>
    public int UntrackedCount { get; init; }

    /// <summary>Un remote nomme <c>origin</c> est configure.</summary>
    public bool HasOrigin { get; init; }

    /// <summary>Nombre de commits locaux en avance sur <c>origin/&lt;branche&gt;</c> ; 0 si pas de pendant distant.</summary>
    public int AheadOfOrigin { get; init; }

    /// <summary>Fichiers contenant des marqueurs de conflit non resolus (chemin relatif).</summary>
    public IReadOnlyList<string> ConflictedFiles { get; init; } = [];

    /// <summary>
    /// Chemins relatifs (separateur <c>/</c>) ayant du travail non committe (indexe / non indexe /
    /// non suivi). Permet l'attribution <b>par exercice</b> (prefixe de chemin) plutot que repo-wide.
    /// </summary>
    public IReadOnlyList<string> UncommittedPaths { get; init; } = [];

    /// <summary>
    /// Chemins relatifs (separateur <c>/</c>) modifies par les commits en avance sur
    /// <c>origin/&lt;branche&gt;</c> (diff <c>origin..HEAD</c>). Permet l'attribution <b>par exercice</b>.
    /// </summary>
    public IReadOnlyList<string> AheadPaths { get; init; } = [];

    /// <summary>Derive : du travail non committe existe (indexe, non indexe ou non suivi).</summary>
    public bool HasUncommittedWork => StagedCount > 0 || UnstagedCount > 0 || UntrackedCount > 0;
}
