using System;
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

    public GraderResult Grade(GradingContext context, GradingStep step) =>
        throw new NotImplementedException("Implémenté en Task 4.");

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
