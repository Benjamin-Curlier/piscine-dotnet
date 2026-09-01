using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Vérifie des contraintes pédagogiques simples sans exécuter le code.</summary>
public sealed class SourceGrader : IGrader
{
    public string Type => "source";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        if (step.Source is null
            || (step.Source.RequiredFragments.Count == 0
                && step.Source.ForbiddenFragments.Count == 0
                && step.Source.RequiredOccurrences.Count == 0))
        {
            return GraderResult.Failure(Type,
                "contenu : étape source sans fragment requis ni interdit.");
        }

        var messages = new List<string>();
        var comparableSources = context.Sources.ToDictionary(
            source => source.Key,
            source => ComparableSource(source.Value),
            StringComparer.Ordinal);
        foreach (var forbidden in step.Source.ForbiddenFragments.Where(fragment => !string.IsNullOrWhiteSpace(fragment)))
        {
            var normalized = ComparableFragment(forbidden);
            var files = comparableSources
                .Where(source => source.Value.Contains(normalized, StringComparison.Ordinal))
                .Select(source => source.Key)
                .ToList();
            if (files.Count > 0)
            {
                messages.Add($"Fragment interdit « {forbidden} » trouvé dans {string.Join(", ", files)}.");
            }
        }

        foreach (var required in step.Source.RequiredFragments.Where(fragment => !string.IsNullOrWhiteSpace(fragment)))
        {
            var normalized = ComparableFragment(required);
            if (!comparableSources.Values.Any(source => source.Contains(normalized, StringComparison.Ordinal)))
            {
                messages.Add($"Fragment requis absent : « {required} ».");
            }
        }

        var allSources = string.Concat(comparableSources.Values);
        foreach (var occurrence in step.Source.RequiredOccurrences
                     .Where(occurrence => !string.IsNullOrWhiteSpace(occurrence.Fragment)))
        {
            if (occurrence.Count <= 0)
            {
                messages.Add($"Contenu invalide : le nombre requis pour « {occurrence.Fragment} » doit être positif.");
                continue;
            }

            var normalized = ComparableFragment(occurrence.Fragment);
            var actual = CountOccurrences(allSources, normalized);
            if (actual < occurrence.Count)
            {
                messages.Add(
                    $"Fragment « {occurrence.Fragment} » présent {actual} fois ; {occurrence.Count} occurrence(s) requise(s).");
            }
        }

        return messages.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.SourceConstraint);
    }

    private static string ComparableSource(string source) => string.Concat(
        CSharpSyntaxTree.ParseText(source).GetRoot().DescendantTokens().Select(token => token.Text));

    private static string ComparableFragment(string fragment) => string.Concat(fragment.Where(character => !char.IsWhiteSpace(character)));

    private static int CountOccurrences(string source, string fragment)
    {
        if (fragment.Length == 0)
        {
            return 0;
        }

        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += fragment.Length;
        }

        return count;
    }
}
