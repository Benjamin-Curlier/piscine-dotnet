using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Piscine.Sandbox;

/// <summary>
/// Point d'entrée logique du bac à sable — processus NON fiable : il exécute le code recrue. Il lit
/// le dossier de travail, exécute, puis ÉMET le verdict sous forme de trame sur stdout ; il n'écrit
/// AUCUN fichier de résultat. Le résultat autoritaire est dérivé par le parent de confiance
/// (Piscine.Grading), seul à observer cette trame. La recrue partage ce processus mais n'a donc
/// aucun fichier de résultat à falsifier (cf. correctif d'intégrité B-2).
/// </summary>
public static class SandboxEntry
{
    public static int Run(string workDir)
    {
        var request = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(workDir, "request.json")),
            SandboxJsonContext.Default.SandboxRequest)!;
        var bytes = File.ReadAllBytes(Path.Combine(workDir, "asm.dll"));

        // Écrivain vers le VRAI stdout, capturé AVANT toute exécution : en mode io, RunIo échange
        // Console.Out vers un StringWriter et ne le restaure que dans un finally jamais atteint sous
        // Environment.Exit. La trame doit sortir sur le pipe réel, pas dans la capture in-memory.
        var frameOut = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
        {
            AutoFlush = false,
        };

        var result = new SandboxResult();
        var emitted = false;
        void Emit()
        {
            if (emitted)
            {
                return;
            }

            emitted = true;
            frameOut.Write(SandboxProtocol.VerdictSentinel);
            frameOut.Write(JsonSerializer.Serialize(result, SandboxJsonContext.Default.SandboxResult));
            frameOut.Write('\n');
            frameOut.Flush();
        }

        // Si la recrue appelle Environment.Exit(n), l'exécution ne revient pas : on émet un résultat
        // partiel marqué ExitedEarly pour que le parent y recolle le code de sortie réel (et ne le
        // prenne pas pour un crash). FailFast, lui, saute ProcessExit ⇒ aucune trame ⇒ le parent
        // signale un arrêt anormal (fail-closed).
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            // Le finally de restauration dans RunIo n'a PAS tourné (Environment.Exit court-circuite
            // tout) : on récupère le stdout io déjà produit via le writer de capture exposé, sinon un
            // programme correct qui termine par Environment.Exit émettrait une sortie vide → faux
            // « À revoir » (M-4). (Console.Out est un wrapper synchronisé, d'où l'accès direct.)
            if (string.IsNullOrEmpty(result.Stdout) && SandboxExecutor.CurrentIoCapture is { } captured)
            {
                result.Stdout = captured.ToString();
            }

            result.ExitedEarly = true;
            Emit();
        };

        try
        {
            result = SandboxExecutor.Execute(request, bytes);
        }
        catch (Exception ex)
        {
            result = new SandboxResult { ErrorType = ex.GetType().Name, ErrorMessage = ex.Message };
        }

        Emit();
        return 0;
    }
}
