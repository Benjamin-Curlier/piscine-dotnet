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

    /// <summary>Résultat d'une exécution : nombre de tests trouvés, échecs, et drapeau de timeout.</summary>
    public sealed record RunResult(int FactCount, IReadOnlyList<string> Failures, bool TimedOut);

    public static RunResult Run(byte[] assemblyBytes, TimeSpan timeout)
    {
        var request = new SandboxRequest
        {
            Mode = "xunit",
            ReferencePaths = System.Linq.Enumerable.ToArray(CompilationService.ReferenceAssemblyPaths),
        };
        var result = SandboxProcess.Run(request, assemblyBytes, timeout, out var timedOut);
        return timedOut
            ? new RunResult(0, Array.Empty<string>(), true)
            : new RunResult(result.FactCount, result.Failures, false);
    }
}
