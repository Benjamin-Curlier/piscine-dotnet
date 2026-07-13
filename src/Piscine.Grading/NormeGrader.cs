using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>norme</c> : compare chaque fichier au formatage canonique Roslyn.
/// Non bloquant par défaut (advisory) ; bloquant si <see cref="GradingStep.Blocking"/>.
/// </summary>
public sealed class NormeGrader : IGrader
{
    public string Type => "norme";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var messages = new List<string>();

        foreach (var (fileName, source) in context.Sources)
        {
            if (!IsCanonical(source))
            {
                messages.Add($"{fileName} : le formatage diffère de la norme (indentation, espaces, accolades). Reformate avec ton éditeur (ou dotnet format) pour te conformer.");
            }
        }

        if (messages.Count == 0)
        {
            return GraderResult.Success(Type);
        }

        return step.Blocking
            ? GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.NormeViolation)
            : GraderResult.Advisory(Type, messages.ToArray());
    }

    private static bool IsCanonical(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var formatted = Formatter.Format(root, workspace).ToFullString();
        return Normalize(formatted) == Normalize(source);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd();
}
