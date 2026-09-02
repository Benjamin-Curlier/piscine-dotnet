using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                && step.Source.RequiredOccurrences.Count == 0
                && step.Source.RequiredInvocations.Count == 0
                && step.Source.ForbiddenInvocations.Count == 0
                && step.Source.RequiredSyntax.Count == 0
                && step.Source.RequiredDeclaredTypes.Count == 0
                && step.Source.RequiredBaseTypes.Count == 0
                && !step.Source.RequireCompilation))
        {
            return GraderResult.Failure(Type,
                "contenu : étape source sans fragment requis ni interdit.");
        }

        var messages = new List<string>();
        AnalyzeSemantics(context, step.Source, messages);
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

        foreach (var occurrence in step.Source.RequiredOccurrences
                     .Where(occurrence => !string.IsNullOrWhiteSpace(occurrence.Fragment)))
        {
            if (occurrence.Count <= 0)
            {
                messages.Add($"Contenu invalide : le nombre requis pour « {occurrence.Fragment} » doit être positif.");
                continue;
            }

            var normalized = ComparableFragment(occurrence.Fragment);
            // Compter fichier par fichier : concaténer les sources pouvait fabriquer une occurrence
            // inexistante à cheval sur la fin d'un fichier et le début du suivant.
            var actual = comparableSources.Values.Sum(source => CountOccurrences(source, normalized));
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

    private static void AnalyzeSemantics(
        GradingContext context,
        SourceAssertions assertions,
        List<string> messages)
    {
        var semanticRequested = assertions.RequireCompilation
            || assertions.RequiredInvocations.Count > 0
            || assertions.ForbiddenInvocations.Count > 0
            || assertions.RequiredSyntax.Count > 0
            || assertions.RequiredDeclaredTypes.Count > 0
            || assertions.RequiredBaseTypes.Count > 0;
        if (!semanticRequested)
        {
            return;
        }

        var outputKind = CompilationService.HasTopLevelStatements(context.Sources.Values)
            ? OutputKind.ConsoleApplication
            : OutputKind.DynamicallyLinkedLibrary;
        var compilation = CompilationService.CreateCompilation(context.Sources, outputKind);
        var emitted = CompilationService.Emit(compilation);
        if (!emitted.Success)
        {
            messages.Add("Le code doit compiler pour vérifier la technique demandée.");
            messages.AddRange(emitted.Errors);
            return;
        }

        var invocations = new HashSet<string>(StringComparer.Ordinal);
        var declaredTypes = new List<INamedTypeSymbol>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol resolved)
                {
                    var method = resolved.ReducedFrom ?? resolved;
                    invocations.Add($"{method.ContainingType.ToDisplayString()}.{method.Name}");
                }
            }

            foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration) is INamedTypeSymbol symbol)
                {
                    declaredTypes.Add(symbol);
                }
            }
        }

        foreach (var required in assertions.RequiredInvocations.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!invocations.Contains(required))
            {
                messages.Add($"Appel requis absent : « {required} ».");
            }
        }

        foreach (var forbidden in assertions.ForbiddenInvocations.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (invocations.Contains(forbidden))
            {
                messages.Add($"Appel interdit présent : « {forbidden} ».");
            }
        }

        foreach (var required in assertions.RequiredDeclaredTypes.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!declaredTypes.Any(type => MatchesType(type, required)))
            {
                messages.Add($"Type déclaré requis absent : « {required} ».");
            }
        }

        foreach (var required in assertions.RequiredBaseTypes.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!declaredTypes.Any(type => BaseTypes(type).Any(baseType => MatchesType(baseType, required))))
            {
                messages.Add($"Aucun type ne dérive de ou n'implémente « {required} ».");
            }
        }

        foreach (var syntax in assertions.RequiredSyntax.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!HasSyntax(compilation.SyntaxTrees, syntax))
            {
                messages.Add($"Construction C# requise absente : « {syntax} ».");
            }
        }
    }

    private static bool MatchesType(INamedTypeSymbol type, string expected) =>
        string.Equals(type.Name, expected, StringComparison.Ordinal)
        || string.Equals(type.ToDisplayString(), expected, StringComparison.Ordinal)
        || string.Equals(type.OriginalDefinition.ToDisplayString(), expected, StringComparison.Ordinal);

    private static IEnumerable<INamedTypeSymbol> BaseTypes(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }

        foreach (var contract in type.AllInterfaces)
        {
            yield return contract;
        }
    }

    private static bool HasSyntax(IEnumerable<SyntaxTree> trees, string required)
    {
        foreach (var root in trees.Select(tree => tree.GetRoot()))
        {
            var found = required switch
            {
                "lock" => root.DescendantNodes().OfType<LockStatementSyntax>().Any(),
                "await" => root.DescendantNodes().OfType<AwaitExpressionSyntax>().Any(),
                "await-foreach" => root.DescendantNodes().OfType<ForEachStatementSyntax>()
                    .Any(statement => !statement.AwaitKeyword.IsKind(SyntaxKind.None)),
                "try-catch" => root.DescendantNodes().OfType<CatchClauseSyntax>().Any(),
                _ => false,
            };
            if (found)
            {
                return true;
            }
        }

        return false;
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
