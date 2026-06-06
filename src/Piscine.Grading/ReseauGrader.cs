using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>reseau</c> : démarre un **serveur de test embarqué** (écho TCP déterministe), exécute le
/// programme de la recrue en lui passant <c>host</c>/<c>port</c> en arguments, et compare la sortie
/// standard / le code de sortie aux attentes. Rend les exercices réseau reproductibles.
/// </summary>
public sealed class ReseauGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Type => "reseau";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var mode = step.Network?.Mode ?? "echo";
        if (mode != "echo")
        {
            return GraderResult.Failure(Type, $"contenu : mode de serveur de test inconnu « {mode} » (attendu : echo).");
        }

        var compilation = CompilationService.Compile(context.Sources, OutputKind.ConsoleApplication);
        if (!compilation.Success)
        {
            var messages = new List<string> { "Le programme ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        foreach (var ioCase in step.Cases)
        {
            using var harness = NetworkHarness.StartEcho();

            var args = new List<string> { harness.Host, harness.Port.ToString() };
            args.AddRange(ioCase.Args);

            var run = ProgramRunner.Run(compilation.AssemblyBytes, args.ToArray(), ioCase.Stdin, Timeout);

            if (run.TimedOut)
            {
                return GraderResult.Failure(Type, "Ton programme ne s'est pas terminé à temps (attente réseau ou boucle infinie ?).")
                    .WithTrigger(FeedbackTriggers.Timeout);
            }

            if (run.Error is not null)
            {
                return GraderResult.Failure(Type, $"Ton programme a levé une exception : {run.Error.GetType().Name} — {run.Error.Message}")
                    .WithTrigger(FeedbackTriggers.RuntimeError);
            }

            if (Normalize(run.Stdout) != Normalize(ioCase.ExpectStdout))
            {
                return GraderResult.Failure(
                    Type,
                    "La sortie ne correspond pas.",
                    $"Attendu : {Quote(ioCase.ExpectStdout)}",
                    $"Obtenu  : {Quote(run.Stdout)}").WithTrigger(FeedbackTriggers.IoMismatch);
            }

            if (run.ExitCode != ioCase.ExpectExit)
            {
                return GraderResult.Failure(
                    Type,
                    $"Code de sortie inattendu : attendu {ioCase.ExpectExit}, obtenu {run.ExitCode}.")
                    .WithTrigger(FeedbackTriggers.ExitCode);
            }
        }

        return GraderResult.Success(Type);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\n", "\\n") + "\"";
}
