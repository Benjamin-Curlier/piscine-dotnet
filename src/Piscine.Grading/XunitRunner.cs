using System;
using System.Collections.Generic;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>
/// Exécute les méthodes <c>[Fact]</c> d'un assembly compilé dans un PROCESSUS ENFANT jetable
/// (Piscine.Sandbox), tué au timeout (arbre complet). Partagé par les graders <c>unit</c> et
/// <c>mutation</c>. La mort du processus récupère thread et assembly ; les fixtures de test sont
/// disposées dans le bac à sable.
/// </summary>
internal static class XunitRunner
{
    /// <summary>Chemins des assemblies xUnit à passer en références de compilation.</summary>
    public static readonly string[] References =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location,
    };

    /// <summary>
    /// Résultat d'une exécution : nombre de tests trouvés, échecs, drapeau de timeout, et drapeau
    /// <paramref name="Crashed"/> (arrêt anormal du bac à sable — ex. StackOverflow — sans result.json).
    /// Un run est « propre » (vert exploitable) seulement si <c>!TimedOut &amp;&amp; !Crashed</c>.
    /// </summary>
    public sealed record RunResult(int FactCount, IReadOnlyList<string> Failures, bool TimedOut, bool Crashed = false)
    {
        /// <summary>Vrai si le processus a mené les tests à terme normalement (ni timeout ni crash).</summary>
        public bool RanCleanly => !TimedOut && !Crashed;
    }

    public static RunResult Run(byte[] assemblyBytes, TimeSpan timeout)
    {
        var request = new SandboxRequest
        {
            Mode = "xunit",
            ReferencePaths = System.Linq.Enumerable.ToArray(CompilationService.ReferenceAssemblyPaths),
        };
        var result = SandboxProcess.Run(request, assemblyBytes, timeout, out var timedOut);
        if (timedOut)
        {
            return new RunResult(0, Array.Empty<string>(), true);
        }

        // ErrorType non nul sans timeout = arrêt anormal (result.json absent/partiel) : run non fiable.
        var crashed = result.ErrorType is not null;
        return new RunResult(result.FactCount, result.Failures, false, crashed);
    }
}
