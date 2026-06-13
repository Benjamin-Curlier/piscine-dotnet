namespace Piscine.App.Terminal;

/// <summary>
/// Parametres de coalescence de la sortie PTY.
/// La sortie brute est bufferisee puis emise en un seul evenement :
/// <list type="bullet">
///   <item>au plus toutes les <see cref="FlushInterval"/> (fenetre temporelle),</item>
///   <item>ou des que le buffer accumule <see cref="MaxBufferBytes"/> octets (seuil de taille).</item>
/// </list>
/// Aucun octet n'est perdu ; l'ordre est preserve.
/// <br/>
/// Les valeurs par defaut (&lt;= 16 ms / 32 Ko) offrent une fluidite visuelle ~60 fps sans surcharger
/// le pont JS/interop meme sur une sortie tres verbeuse (<c>ls -R</c>, <c>dir /s</c>).
/// </summary>
public sealed record PtyCoalescerOptions
{
    /// <summary>
    /// Fenetre temporelle maximale entre deux evenements <c>Output</c>.
    /// Une sortie rapide est regroupee ; une sortie isolee est emise en moins de <see cref="FlushInterval"/>.
    /// Par defaut : 16 ms.
    /// </summary>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMilliseconds(16);

    /// <summary>
    /// Seuil de taille du buffer (en octets) qui declenche un flush immediat avant la fin de la fenetre.
    /// Evite d'accumuler une quantite de memoire illimitee sur une sortie tres rapide.
    /// Par defaut : 32 768 octets (32 Ko).
    /// </summary>
    public int MaxBufferBytes { get; init; } = 32 * 1024;

    /// <summary>
    /// Fournisseur de temps utilise pour le timer de flush.
    /// Injecter un <see cref="FakeTimeProvider"/> (Microsoft.Extensions.TimeProvider.Testing) en test
    /// pour controler le temps sans attendre.
    /// <c>null</c> = <see cref="TimeProvider.System"/> (comportement production).
    /// </summary>
    public TimeProvider? TimeProvider { get; init; }
}
