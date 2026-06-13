using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>reseau</c> : démarre un **serveur de test embarqué**, exécute le programme de la recrue
/// en lui passant <c>host</c>/<c>port</c> (mode TCP) ou <c>baseUrl</c> (mode HTTP) en arguments, et
/// compare la sortie standard / le code de sortie aux attentes. Rend les exercices réseau reproductibles.
/// <list type="bullet">
///   <item>Mode <c>echo</c> — écho TCP : injecte <c>host port</c> en args[0]/args[1].</item>
///   <item>Mode <c>http</c> — serveur HTTP (<c>HttpListener</c>) : injecte <c>baseUrl</c> en args[0].</item>
/// </list>
/// </summary>
public sealed class ReseauGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static readonly HashSet<string> KnownModes = new(StringComparer.Ordinal) { "echo", "http" };

    public string Type => "reseau";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var mode = step.Network?.Mode ?? "echo";
        if (!KnownModes.Contains(mode))
        {
            return GraderResult.Failure(Type, $"contenu : mode de serveur de test inconnu « {mode} » (attendu : echo | http).");
        }

        // Fail-closed : une étape reseau sans cas n'exécute aucune vérification → faux « réussi ».
        if (step.Cases.Count == 0)
        {
            return GraderResult.Failure(Type, "contenu : étape reseau sans cas d'exécution (vérification vide).");
        }

        // Validation spécifique au mode http.
        if (mode == "http" && (step.Network is null || step.Network.Routes.Count == 0))
        {
            return GraderResult.Failure(Type, "contenu : étape reseau http sans routes configurées (vérification vide).");
        }

        var compilation = CompilationService.Compile(context.Sources, OutputKind.ConsoleApplication);
        if (!compilation.Success)
        {
            var messages = new List<string> { "Le programme ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        return mode == "http"
            ? GradeHttp(compilation, step)
            : GradeEcho(compilation, step);
    }

    // ── mode echo (TCP) ───────────────────────────────────────────────────────

    private GraderResult GradeEcho(CompilationResult compilation, GradingStep step)
    {
        foreach (var ioCase in step.Cases)
        {
            using var harness = NetworkHarness.StartEcho();

            var args = new List<string>
            {
                harness.Host,
                harness.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            args.AddRange(ioCase.Args);

            var run = ProgramRunner.Run(compilation.AssemblyBytes, args.ToArray(), ioCase.Stdin, Timeout);
            var check = CheckRun(run, ioCase);
            if (check is not null)
            {
                return check;
            }
        }

        return GraderResult.Success(Type);
    }

    // ── mode http ─────────────────────────────────────────────────────────────

    private GraderResult GradeHttp(CompilationResult compilation, GradingStep step)
    {
        var routes = step.Network!.Routes;
        foreach (var ioCase in step.Cases)
        {
            using var harness = NetworkHarness.StartHttp(routes);

            var args = new List<string> { harness.BaseUrl! };
            args.AddRange(ioCase.Args);

            var run = ProgramRunner.Run(compilation.AssemblyBytes, args.ToArray(), ioCase.Stdin, Timeout);
            var check = CheckRun(run, ioCase);
            if (check is not null)
            {
                return check;
            }
        }

        return GraderResult.Success(Type);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Vérifie timeout / exception / stdout / exit. Retourne un échec ou null (succès).</summary>
    private GraderResult? CheckRun(RunOutcome run, IoCase ioCase)
    {
        if (run.TimedOut)
        {
            return GraderResult.Failure(Type, "Ton programme ne s'est pas terminé à temps (attente réseau ou boucle infinie ?).")
                .WithTrigger(FeedbackTriggers.Timeout);
        }

        if (run.Error is not null)
        {
            return GraderResult.Failure(Type, $"Ton programme a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
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

        return null;
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\n", "\\n") + "\"";
}
