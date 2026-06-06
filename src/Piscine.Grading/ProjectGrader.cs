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
                return GraderResult.Failure(Type, $"Le projet a levé une exception : {run.Error.GetType().Name} — {run.Error.Message}")
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

            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (model.GetSymbolInfo(name).Symbol is not INamedTypeSymbol referenced)
                {
                    continue;
                }

                var referencedNs = NamespaceOf(referenced.ContainingNamespace);
                if (referencedNs.Length == 0)
                {
                    continue;
                }

                var fromNs = EnclosingNamespace(name);

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

    /// <summary>Namespace de déclaration entourant un nœud (file-scoped ou en bloc), ou chaîne vide (global).</summary>
    private static string EnclosingNamespace(SyntaxNode node)
    {
        var ns = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return ns?.Name.ToString() ?? string.Empty;
    }

    private static string NamespaceOf(INamespaceSymbol? ns) =>
        ns is null || ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();

    /// <summary>Vrai si <paramref name="ns"/> est <paramref name="prefix"/> ou un sous-namespace.</summary>
    private static bool NamespaceMatches(string ns, string prefix) =>
        ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal);

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\n", "\\n") + "\"";
}
