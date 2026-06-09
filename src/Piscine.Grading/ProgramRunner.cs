using System;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>Erreur recrue rapportée à travers la frontière du bac à sable (type + message).</summary>
public sealed record RunError(string TypeName, string Message);

/// <summary>Issue d'une exécution isolée d'un assembly compilé.</summary>
public sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, RunError? Error);

/// <summary>
/// Exécute un assembly console compilé dans un PROCESSUS ENFANT jetable (Piscine.Sandbox), tué au
/// timeout (arbre de processus complet). Partagé par les graders io/projet/reseau et l'outil auteur
/// <c>piscine try</c>. La mort du processus récupère thread, assembly et capture de sortie ; aucune
/// mutation du <c>Console</c> global côté parent, donc aucune contamination inter-exécutions.
/// </summary>
public static class ProgramRunner
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static RunOutcome Run(byte[] assemblyBytes, string[] args, string stdin, TimeSpan? timeout = null)
    {
        var request = new SandboxRequest { Mode = "io", Args = args, Stdin = stdin };
        var result = SandboxProcess.Run(request, assemblyBytes, timeout ?? DefaultTimeout, out var timedOut);
        if (timedOut)
        {
            return new RunOutcome(string.Empty, 0, true, null);
        }

        var error = result.ErrorType is null
            ? null
            : new RunError(result.ErrorType, result.ErrorMessage ?? string.Empty);
        return new RunOutcome(result.Stdout, result.ExitCode, false, error);
    }
}
