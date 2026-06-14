using System.Collections.Generic;

namespace Piscine.App.Launch;

/// <summary>Description d'un lancement de processus (commande + arguments, jamais concaténés).</summary>
public sealed record LaunchSpec(string FileName, IReadOnlyList<string> Arguments);

/// <summary>Abstraction du lancement de processus OS — permet d'asserter la commande sans spawn réel.</summary>
public interface IProcessLauncher
{
    /// <summary>Lance le processus détaché (best-effort). Renvoie true si démarré.</summary>
    bool Launch(LaunchSpec spec);
}
