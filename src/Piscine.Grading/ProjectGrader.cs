using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>projet</c> : corrige une solution **multi-fichiers / multi-couches**. Compile tous les
/// livrables ensemble, exécute d'éventuels cas io, puis vérifie des **assertions d'architecture**
/// (types requis présents, dépendances de couches interdites) via l'analyse sémantique Roslyn.
/// </summary>
public sealed class ProjectGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Type => "projet";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var hasCases = step.Cases.Count > 0;

        var authoring = ValidateAuthoring(step, hasCases);
        if (authoring is not null)
        {
            return GraderResult.Failure(Type, authoring);
        }

        var outputKind = hasCases ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
        var compilation = CompilationService.CreateCompilation(context.Sources, outputKind);

        var emitted = CompilationService.Emit(compilation);
        if (!emitted.Success)
        {
            var messages = new List<string> { "Le projet ne compile pas :" };
            messages.AddRange(emitted.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        // Correction comportementale d'abord (cas io), si déclarés.
        if (hasCases)
        {
            var ioResult = RunCases(emitted.AssemblyBytes, step);
            if (ioResult is not null)
            {
                return ioResult;
            }
        }

        // Assertions d'architecture.
        var failures = new List<string>();
        if (step.Project is not null)
        {
            CheckRequiredTypes(compilation, step.Project, failures);
            CheckForbiddenDependencies(compilation, step.Project, failures);
        }

        if (failures.Count == 0)
        {
            return GraderResult.Success(Type);
        }

        var report = new List<string> { "L'architecture de ta solution ne respecte pas les règles :" };
        report.AddRange(failures.Select(f => $"- {f}"));
        return GraderResult.Failure(Type, report.ToArray()).WithTrigger(FeedbackTriggers.ProjectStructure);
    }

    /// <summary>Détecte les erreurs d'auteur (manifest) ; renvoie un message ou <c>null</c> si l'étape est valide.</summary>
    private static string? ValidateAuthoring(GradingStep step, bool hasCases)
    {
        var hasAssertions = step.Project is not null
            && (step.Project.RequiresTypes.Count > 0 || step.Project.ForbiddenDependencies.Count > 0);
        if (!hasCases && !hasAssertions)
        {
            return "contenu : étape projet sans cas io ni assertion d'architecture (vérification vide).";
        }

        if (step.Project is not null)
        {
            foreach (var rule in step.Project.ForbiddenDependencies)
            {
                if (string.IsNullOrWhiteSpace(rule.From) || string.IsNullOrWhiteSpace(rule.To))
                {
                    return "contenu : règle de couche incomplète (from/to vide).";
                }
            }
        }

        return null;
    }

    /// <summary>Exécute les cas io ; renvoie un échec au 1er écart, ou <c>null</c> si tout passe.</summary>
    private GraderResult? RunCases(byte[] assemblyBytes, GradingStep step)
    {
        foreach (var ioCase in step.Cases)
        {
            var run = ProgramRunner.Run(assemblyBytes, ioCase.Args.ToArray(), ioCase.Stdin, Timeout);

            if (run.TimedOut)
            {
                return GraderResult.Failure(Type, "Le projet ne s'est pas terminé à temps (boucle infinie ?).")
                    .WithTrigger(FeedbackTriggers.Timeout);
            }

            if (run.Error is not null)
            {
                return GraderResult.Failure(Type, $"Le projet a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
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

        return null;
    }

    private static void CheckRequiredTypes(CSharpCompilation compilation, ProjectAssertions project, List<string> failures)
    {
        foreach (var fqn in project.RequiresTypes)
        {
            if (compilation.GetTypeByMetadataName(fqn) is null)
            {
                failures.Add($"le type requis « {fqn} » est introuvable.");
            }
        }
    }

    private static void CheckForbiddenDependencies(CSharpCompilation compilation, ProjectAssertions project, List<string> failures)
    {
        if (project.ForbiddenDependencies.Count == 0)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            // DescendantNodes() ne descend pas dans la trivia : les cref de doc XML (/// <see cref="…"/>)
            // sont exclus, on ne compte donc que les vraies références de code.
            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                var referenced = ReferencedType(model.GetSymbolInfo(name).Symbol);
                if (referenced is null)
                {
                    continue;
                }

                var referencedNs = NamespaceOf(referenced.ContainingNamespace);
                if (referencedNs.Length == 0)
                {
                    continue;
                }

                var fromNs = EnclosingNamespace(model, name);

                foreach (var rule in project.ForbiddenDependencies)
                {
                    if (NamespaceMatches(fromNs, rule.From) && NamespaceMatches(referencedNs, rule.To))
                    {
                        var message = $"un type de « {rule.From} » référence « {rule.To} » ({referenced.Name}) : couche interdite.";
                        if (seen.Add(message))
                        {
                            failures.Add(message);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Type porteur d'une référence, aux fins de l'analyse de couche. Un type nommé (héritage, <c>new</c>,
    /// <c>typeof</c>, nom pleinement qualifié, alias, <c>global::</c>…) se rapporte à lui-même. Une
    /// dépendance qui ne NOMME jamais le type — méthode d'extension, <c>using static</c>, accès de membre
    /// via <c>var</c>… — se rapporte au type qui DÉCLARE le membre résolu (méthode, propriété, champ,
    /// événement). Pour une méthode d'extension appelée en forme réduite (<c>x.Ext()</c>), on remonte à la
    /// classe statique d'origine via <see cref="IMethodSymbol.ReducedFrom"/>. <c>null</c> = aucune
    /// dépendance de type pertinente (namespace, paramètre, variable locale, littéral…).
    /// </summary>
    private static INamedTypeSymbol? ReferencedType(ISymbol? symbol) => symbol switch
    {
        INamedTypeSymbol type => type,
        IMethodSymbol method => method.ReducedFrom?.ContainingType ?? method.ContainingType,
        IPropertySymbol property => property.ContainingType,
        IFieldSymbol field => field.ContainingType,
        IEventSymbol @event => @event.ContainingType,
        _ => null,
    };

    /// <summary>
    /// Namespace du type qui contient la référence — déterminé via le symbole du type englobant
    /// (gère types imbriqués et namespaces multiples par fichier), avec repli syntaxique pour le code
    /// hors type (top-level statements). Chaîne vide = namespace global.
    /// </summary>
    private static string EnclosingNamespace(SemanticModel model, SyntaxNode node)
    {
        var typeDecl = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault();
        if (typeDecl is not null && model.GetDeclaredSymbol(typeDecl) is INamedTypeSymbol typeSymbol)
        {
            return NamespaceOf(typeSymbol.ContainingNamespace);
        }

        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString() ?? string.Empty;
    }

    private static string NamespaceOf(INamespaceSymbol? ns) =>
        ns is null || ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();

    /// <summary>Vrai si <paramref name="ns"/> est <paramref name="prefix"/> ou un sous-namespace.</summary>
    private static bool NamespaceMatches(string ns, string prefix) =>
        ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal);

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    // Échappe \r ET \n (cf. IoGrader.Quote) : un \r brut déplacerait le curseur et corromprait
    // l'alignement « Attendu / Obtenu » dans le terminal.
    private static string Quote(string s) => "\"" + s.Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
}
