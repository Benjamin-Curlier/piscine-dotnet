namespace Piscine.App.Terminal;

/// <summary>
/// Décide si le terminal embarqué (shell OS) peut être lancé selon l'hôte.
/// <list type="bullet">
///   <item><b>Photino</b> (app de bureau locale, in-process) : <c>true</c> — c'est la fonctionnalité voulue.</item>
///   <item><b>Piscine.DevHost</b> (harnais Blazor Server) : <c>true</c> seulement en Development — un shell
///   OS sans authentification ne doit jamais être exposé hors loopback ni livré.</item>
/// </list>
/// Remplace l'ancienne garde <c>IHostEnvironment.IsDevelopment()</c> codée dans la page RCL, que l'hôte
/// Photino ne pouvait pas satisfaire (pas d'<c>IHostEnvironment</c> enregistré).
/// </summary>
public sealed class TerminalPolicy
{
    public TerminalPolicy(bool enabled) => Enabled = enabled;

    /// <summary>Le terminal embarqué est-il autorisé dans cet hôte ?</summary>
    public bool Enabled { get; }
}
