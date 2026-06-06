namespace Piscine.App.Init;

/// <summary>Statut de l'environnement git de la recrue (lecture seule, instantané).</summary>
public record InitStatus(
    bool WorkspaceReady,
    bool BareRepoReady,
    bool HookInstalled,
    bool OriginConfigured)
{
    /// <summary>
    /// Vrai si les trois conditions essentielles sont remplies : dépôt workspace valide,
    /// dépôt bare valide, hook post-receive présent. <see cref="OriginConfigured"/> est
    /// informatif (<c>GitWorkspace.Initialize</c> ajoute la remote <c>origin</c> au dépôt
    /// workspace, pointant vers le bare ; on la détecte séparément pour le débogage).
    /// </summary>
    public bool IsInitialized => WorkspaceReady && BareRepoReady && HookInstalled;
}
