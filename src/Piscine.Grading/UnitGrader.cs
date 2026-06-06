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

        var compilation = CompilationService.Compile(
            sources,
            OutputKind.DynamicallyLinkedLibrary,
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

        if (run.FactCount == 0)
        {
            return GraderResult.Failure(Type, "Aucun test n'a été trouvé.").WithTrigger(FeedbackTriggers.UnitFailure);
        }

        return run.Failures.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, run.Failures.ToArray()).WithTrigger(FeedbackTriggers.UnitFailure);
    }
}
