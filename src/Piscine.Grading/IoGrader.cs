using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>io</c> : compile le programme de la recrue, l'exécute dans un contexte isolé,
/// et compare la sortie standard / le code de sortie aux attentes.
/// </summary>
public sealed class IoGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Type => "io";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        // Fail-closed : une étape io sans cas n'exécute aucune vérification → un rendu qui compile
        // « réussirait » par défaut. On l'interdit (cf. ProjectGrader, même garde de contenu).
        if (step.Cases.Count == 0)
        {
            return GraderResult.Failure(Type, "contenu : étape io sans cas d'exécution (vérification vide).");
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
            var run = ProgramRunner.Run(compilation.AssemblyBytes, ioCase.Args.ToArray(), ioCase.Stdin, Timeout);

            if (run.TimedOut)
            {
                return GraderResult.Failure(Type, "Votre programme ne s'est pas terminé à temps (boucle infinie ?).")
                    .WithTrigger(FeedbackTriggers.Timeout);
            }

            if (run.Error is not null)
            {
                return GraderResult.Failure(Type, $"Votre programme a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
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
