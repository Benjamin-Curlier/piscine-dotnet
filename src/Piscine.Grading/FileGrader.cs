using System;
using System.Collections.Generic;
using System.Linq;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Vérifie des livrables texte sans supposer qu'ils contiennent du C#.</summary>
public sealed class FileGrader : IGrader
{
    public string Type => "fichier";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        if (step.File is null || step.File.Rules.Count == 0)
        {
            return GraderResult.Failure(Type, "contenu : étape fichier sans règle.");
        }

        var messages = new List<string>();
        foreach (var rule in step.File.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Path))
            {
                messages.Add("Contenu invalide : règle fichier sans chemin.");
                continue;
            }

            if (!context.Sources.TryGetValue(rule.Path, out var content))
            {
                messages.Add($"Fichier requis absent : {rule.Path}.");
                continue;
            }

            foreach (var required in rule.RequiredFragments.Where(fragment => !string.IsNullOrWhiteSpace(fragment)))
            {
                if (!content.Contains(required, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add($"{rule.Path} : élément requis absent « {required} ».");
                }
            }

            foreach (var forbidden in rule.ForbiddenFragments.Where(fragment => !string.IsNullOrWhiteSpace(fragment)))
            {
                if (content.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    messages.Add($"{rule.Path} : élément interdit présent « {forbidden} ».");
                }
            }
        }

        return messages.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.FileConstraint);
    }
}
