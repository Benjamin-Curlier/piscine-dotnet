using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>unit</c> : compile les sources de la recrue avec des tests xUnit cachés,
/// puis exécute les méthodes <c>[Fact]</c> par réflexion (un échec d'assertion = test KO).
/// </summary>
public sealed class UnitGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public string Type => "unit";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var sources = new Dictionary<string, string>(context.Sources);
        foreach (var (name, content) in context.GraderFiles)
        {
            sources[name] = content;
        }

        // Un livrable-concept est souvent un programme top-level (module 20 : Generic Host, etc.). Il ne
        // peut être compilé qu'en exécutable (CS8805) : on choisit ConsoleApplication dans ce cas, DLL
        // sinon (livrable bibliothèque, ex. une classe utilitaire testée directement). Dans les deux cas
        // le runner xUnit n'exécute que les [Fact] par réflexion — le point d'entrée n'est jamais lancé.
        var outputKind = CompilationService.HasTopLevelStatements(sources.Values)
            ? OutputKind.ConsoleApplication
            : OutputKind.DynamicallyLinkedLibrary;

        var compilation = CompilationService.Compile(
            sources,
            outputKind,
            additionalReferences: XunitRunner.References);

        if (!compilation.Success)
        {
            var messages = new List<string> { "Le code ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        var run = XunitRunner.Run(compilation.AssemblyBytes, Timeout);
        if (run.TimedOut)
        {
            return GraderResult.Failure(Type, "Les tests ne se sont pas terminés à temps (boucle infinie ?).")
                .WithTrigger(FeedbackTriggers.Timeout);
        }

        // Arrêt anormal du bac à sable pendant les tests (StackOverflow, Environment.Exit… non
        // rattrapables dans un [Fact]) : verdict non fiable → échec explicite (fail-closed), aligné sur
        // MutationGrader plutôt que de retomber sur le message trompeur « aucun test trouvé ».
        if (run.Crashed)
        {
            return GraderResult.Failure(Type,
                "Les tests provoquent un arrêt anormal (StackOverflow, Environment.Exit… ?).")
                .WithTrigger(FeedbackTriggers.UnitFailure);
        }

        if (run.FactCount == 0)
        {
            return GraderResult.Failure(Type, "Aucun test n'a été trouvé.").WithTrigger(FeedbackTriggers.UnitFailure);
        }

        return run.Failures.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, run.Failures.ToArray()).WithTrigger(FeedbackTriggers.UnitFailure);
    }
}
