using System;
using System.Collections.Generic;
using System.Linq;

namespace Piscine.App.Checking;

/// <summary>
/// Dérive un <see cref="StructuredDiff"/> à partir des messages verbatim d'un grader io.
/// <para>
/// Le moteur (<c>IoGrader</c>/<c>ProjectGrader</c>/<c>ReseauGrader</c>, <b>gelés</b>) émet, sur un
/// échec de sortie, deux lignes de la forme <c>Attendu : "…"</c> et <c>Obtenu  : "…"</c> où le contenu
/// est entre guillemets et les sauts de ligne échappés en <c>\n</c> (cf. <c>IoGrader.Quote</c>).
/// Ce builder retrouve ces deux lignes, déséchappe le contenu et calcule un diff ligne à ligne (LCS)
/// — entièrement dans la couche App, sans modifier le grader. Renvoie <c>null</c> si le cas n'expose
/// pas d'attendu/obtenu (compilation, exception, code de sortie, git, mutation…).
/// </para>
/// </summary>
public static class StructuredDiffBuilder
{
    private const string ExpectedPrefix = "Attendu";
    private const string ActualPrefix = "Obtenu";

    /// <summary>
    /// Tente de bâtir un diff structuré depuis les <paramref name="messages"/> d'un cas. Renvoie
    /// <c>null</c> s'il n'y a pas à la fois une ligne « Attendu » et une ligne « Obtenu » quotées.
    /// </summary>
    public static StructuredDiff? TryBuild(IReadOnlyList<string> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        string? expectedRaw = null;
        string? actualRaw = null;

        foreach (var message in messages)
        {
            if (expectedRaw is null && message.StartsWith(ExpectedPrefix, StringComparison.Ordinal)
                && TryExtractQuoted(message, out var expected))
            {
                expectedRaw = expected;
            }
            else if (actualRaw is null && message.StartsWith(ActualPrefix, StringComparison.Ordinal)
                && TryExtractQuoted(message, out var actual))
            {
                actualRaw = actual;
            }
        }

        if (expectedRaw is null || actualRaw is null)
        {
            return null;
        }

        var expectedLines = SplitLines(Unescape(expectedRaw));
        var actualLines = SplitLines(Unescape(actualRaw));
        return new StructuredDiff(Diff(expectedLines, actualLines));
    }

    /// <summary>Extrait le contenu entre la première et la dernière paire de guillemets de la ligne.</summary>
    private static bool TryExtractQuoted(string message, out string content)
    {
        var first = message.IndexOf('"');
        var last = message.LastIndexOf('"');
        if (first >= 0 && last > first)
        {
            content = message.Substring(first + 1, last - first - 1);
            return true;
        }

        content = string.Empty;
        return false;
    }

    /// <summary>Déséchappe les séquences produites par <c>Quote</c> : <c>\n</c> → saut de ligne réel.</summary>
    private static string Unescape(string quoted)
        => quoted.Replace("\\n", "\n", StringComparison.Ordinal);

    /// <summary>Découpe en lignes (normalise CRLF). Une chaîne vide donne une seule ligne vide.</summary>
    private static List<string> SplitLines(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();

    /// <summary>
    /// Diff ligne à ligne via plus longue sous-séquence commune (LCS) : les lignes communes sont
    /// du contexte (<see cref="DiffLineKind.Unchanged"/>), les autres sont marquées attendu (manquant)
    /// ou obtenu (en trop). Attendu listé avant obtenu pour un même bloc de différence.
    /// </summary>
    private static List<DiffLine> Diff(List<string> expected, List<string> actual)
    {
        int n = expected.Count;
        int m = actual.Count;

        // Table LCS : lcs[i, j] = longueur de la LCS de expected[i..] et actual[j..].
        var lcs = new int[n + 1, m + 1];
        for (int i = n - 1; i >= 0; i--)
        {
            for (int j = m - 1; j >= 0; j--)
            {
                lcs[i, j] = expected[i] == actual[j]
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var lines = new List<DiffLine>();
        int a = 0, b = 0;
        while (a < n && b < m)
        {
            if (expected[a] == actual[b])
            {
                lines.Add(new DiffLine(DiffLineKind.Unchanged, expected[a]));
                a++;
                b++;
            }
            else if (lcs[a + 1, b] >= lcs[a, b + 1])
            {
                lines.Add(new DiffLine(DiffLineKind.Expected, expected[a]));
                a++;
            }
            else
            {
                lines.Add(new DiffLine(DiffLineKind.Actual, actual[b]));
                b++;
            }
        }

        while (a < n)
        {
            lines.Add(new DiffLine(DiffLineKind.Expected, expected[a]));
            a++;
        }

        while (b < m)
        {
            lines.Add(new DiffLine(DiffLineKind.Actual, actual[b]));
            b++;
        }

        return lines;
    }
}
