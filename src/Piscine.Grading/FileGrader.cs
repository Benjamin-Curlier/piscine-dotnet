using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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

            CheckXml(rule, content, messages);
        }

        return messages.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.FileConstraint);
    }

    private static void CheckXml(FileRule rule, string content, List<string> messages)
    {
        if (rule.RequiredXmlElements.Count == 0)
        {
            return;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(content, LoadOptions.None);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            messages.Add($"{rule.Path} : XML invalide — {exception.Message}");
            return;
        }

        foreach (var assertion in rule.RequiredXmlElements)
        {
            if (string.IsNullOrWhiteSpace(assertion.Path))
            {
                messages.Add($"{rule.Path} : assertion XML sans chemin.");
                continue;
            }

            var candidates = FindElements(document, assertion.Path)
                .Where(element => assertion.Value is null
                    || string.Equals(element.Value.Trim(), assertion.Value, StringComparison.Ordinal))
                .Where(element => assertion.Attributes.All(expected =>
                    string.Equals(
                        element.Attributes().FirstOrDefault(attribute =>
                            attribute.Name.LocalName == expected.Key)?.Value,
                        expected.Value,
                        StringComparison.Ordinal)))
                .ToList();

            if (candidates.Count == 0)
            {
                var expectedValue = assertion.Value is null ? string.Empty : $" = « {assertion.Value} »";
                messages.Add($"{rule.Path} : élément XML requis absent ou incorrect « {assertion.Path} »{expectedValue}.");
            }
        }
    }

    private static IEnumerable<XElement> FindElements(XDocument document, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0 || document.Root is null
            || document.Root.Name.LocalName != segments[0])
        {
            return [];
        }

        IEnumerable<XElement> current = [document.Root];
        foreach (var segment in segments.Skip(1))
        {
            current = current.SelectMany(element =>
                element.Elements().Where(child => child.Name.LocalName == segment));
        }

        return current;
    }
}
