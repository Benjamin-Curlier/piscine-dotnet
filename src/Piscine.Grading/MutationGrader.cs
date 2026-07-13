using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>mutation</c> : la recrue livre des tests xUnit ; le moteur les confronte à une
/// implémentation de référence cachée (doivent passer) puis à des mutants dérivés par find/replace
/// (chaque mutant doit être tué par ≥1 test rouge). Verdict binaire.
/// </summary>
public sealed class MutationGrader : IGrader
{
    public string Type => "mutation";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        if (string.IsNullOrEmpty(step.Reference)
            || !context.GraderFiles.TryGetValue(step.Reference, out var reference))
        {
            return GraderResult.Failure(Type, $"contenu : implémentation de référence introuvable ({step.Reference}).");
        }

        // Passe 1 : les tests doivent compiler et passer sur l'implémentation correcte.
        var refCompile = CompileWith(context.Sources, step.Reference, reference);
        if (!refCompile.Success)
        {
            var messages = new List<string> { "Tes tests ne compilent pas contre l'API :" };
            messages.AddRange(refCompile.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        var refRun = XunitRunner.Run(refCompile.AssemblyBytes, Timeout);
        if (refRun.TimedOut)
        {
            return GraderResult.Failure(Type, "Tes tests ne se terminent pas à temps (boucle infinie ?).")
                .WithTrigger(FeedbackTriggers.Timeout);
        }

        if (refRun.Crashed)
        {
            return GraderResult.Failure(Type,
                "Tes tests provoquent un arrêt anormal (StackOverflow, Environment.Exit… ?) sur l'implémentation correcte.")
                .WithTrigger(FeedbackTriggers.TestsFailOnReference);
        }

        if (refRun.FactCount == 0)
        {
            return GraderResult.Failure(Type, "Aucun test n'a été trouvé. Écris au moins un test.")
                .WithTrigger(FeedbackTriggers.MutantSurvived);
        }

        if (refRun.Failures.Count > 0)
        {
            var messages = new List<string> { "Tes tests échouent sur l'implémentation correcte :" };
            messages.AddRange(refRun.Failures);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.TestsFailOnReference);
        }

        // Passe 2..N : chaque mutant doit être tué (≥1 test rouge).
        var survivors = new List<string>();
        foreach (var mutant in step.Mutants)
        {
            var (mutated, count) = ApplyPatch(reference, mutant.Find, mutant.Replace);
            if (count != 1)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » : « {mutant.Find} » devrait matcher exactement une fois la référence (trouvé {count}).");
            }

            if (mutated == reference)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » : find et replace identiques, la référence est inchangée.");
            }

            var mutCompile = CompileWith(context.Sources, step.Reference, mutated);
            if (!mutCompile.Success)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » ne compile pas : {string.Join(" ; ", mutCompile.Errors)}");
            }

            var mutRun = XunitRunner.Run(mutCompile.AssemblyBytes, Timeout);
            // Le mutant n'est SURVIVANT que si les tests tournent proprement et restent TOUS verts.
            // Tout comportement anormal du mutant est un « tué » : timeout (boucle) ET crash/arrêt
            // anormal (StackOverflow…) sont des détections valides — les compter survivants pénalisait
            // à tort des tests corrects (M-5). FactCount>0 garde contre un run vidé de ses tests.
            if (mutRun.RanCleanly && mutRun.FactCount > 0 && mutRun.Failures.Count == 0)
            {
                survivors.Add(mutant.Label);
            }
        }

        if (survivors.Count > 0)
        {
            var messages = new List<string> { "Des comportements bogués ne sont pas détectés par tes tests :" };
            messages.AddRange(survivors.Select(label => $"- {label}"));
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.MutantSurvived);
        }

        return GraderResult.Success(Type);
    }

    private static CompilationResult CompileWith(
        IReadOnlyDictionary<string, string> studentSources, string referenceName, string referenceSource)
    {
        var sources = new Dictionary<string, string>(studentSources) { [referenceName] = referenceSource };
        return CompilationService.Compile(
            sources, OutputKind.DynamicallyLinkedLibrary, additionalReferences: XunitRunner.References);
    }

    /// <summary>
    /// Applique un remplacement de chaîne ; renvoie la source modifiée et le nombre d'occurrences
    /// de <paramref name="find"/> trouvées (le remplacement n'est effectué que si ce nombre vaut 1).
    /// </summary>
    internal static (string Result, int Count) ApplyPatch(string source, string find, string replace)
    {
        if (string.IsNullOrEmpty(find))
        {
            return (source, 0);
        }

        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(find, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += find.Length;
        }

        if (count != 1)
        {
            return (source, count);
        }

        var position = source.IndexOf(find, StringComparison.Ordinal);
        var result = source[..position] + replace + source[(position + find.Length)..];
        return (result, 1);
    }
}
