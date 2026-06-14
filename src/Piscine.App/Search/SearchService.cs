using System.Globalization;
using System.Text;

namespace Piscine.App.Search;

/// <summary>
/// Moteur de recherche PUR de la palette de commande (sans UI, sans IO) : indexe une liste de
/// <see cref="SearchCommand"/> et classe les résultats pour une requête. Insensible à la casse et aux
/// accents. Le filtrage combine :
/// <list type="bullet">
///   <item>une correspondance <b>flou/sous-séquence</b> sur le titre (les lettres de la requête
///   apparaissent dans l'ordre) — fortement pondérée ;</item>
///   <item>une sous-chaîne sur titre/sous-titre/mots-clés — pondérée selon l'emplacement
///   (préfixe &gt; début de mot &gt; ailleurs) ;</item>
///   <item>la <b>recherche plein-texte</b> sur le corps (markdown cours/sujet), faiblement pondérée,
///   avec extraction d'un extrait de contexte.</item>
/// </list>
/// Requête vide → renvoie l'index dans l'ordre « naturel » (destinations/actions d'abord) borné par
/// <paramref name="limit"/>, pour un état initial utile.
/// </summary>
public sealed class SearchService(IReadOnlyList<SearchCommand> index)
{
    private static readonly CompareInfo CompareInfo = CultureInfo.InvariantCulture.CompareInfo;

    private readonly IReadOnlyList<SearchCommand> _index = index ?? [];

    public IReadOnlyList<SearchCommand> Index => _index;

    /// <summary>Classe l'index pour <paramref name="query"/> ; au plus <paramref name="limit"/> résultats.</summary>
    public IReadOnlyList<SearchResult> Search(string? query, int limit = 30)
    {
        if (limit <= 0)
        {
            return [];
        }

        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return _index
                .Select(c => new SearchResult(c, KindWeight(c.Kind), null))
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Command.Title, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
        }

        var needle = Normalize(trimmed);

        var results = new List<SearchResult>(_index.Count);
        foreach (var command in _index)
        {
            if (TryScore(command, needle, out var score, out var snippet))
            {
                results.Add(new SearchResult(command, score, snippet));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Command.Title.Length)
            .ThenBy(r => r.Command.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static bool TryScore(SearchCommand command, string needle, out int score, out string? snippet)
    {
        score = 0;
        snippet = null;

        var title = Normalize(command.Title);
        var subtitle = command.Subtitle is null ? string.Empty : Normalize(command.Subtitle);
        var keywords = command.Keywords is null
            ? string.Empty
            : Normalize(string.Join(' ', command.Keywords));

        var best = 0;

        // 1) Sous-chaîne dans le titre — l'indicateur le plus fort.
        var titleIndex = title.IndexOf(needle, StringComparison.Ordinal);
        if (titleIndex >= 0)
        {
            var positionScore = titleIndex == 0
                ? 1000                               // préfixe exact du titre
                : IsWordStart(title, titleIndex)
                    ? 700                            // début d'un mot du titre
                    : 500;                           // au milieu d'un mot
            best = Math.Max(best, positionScore);
        }

        // 2) Sous-chaîne dans le sous-titre / les mots-clés (identifiants, synonymes).
        if (subtitle.Contains(needle, StringComparison.Ordinal))
        {
            best = Math.Max(best, 400);
        }

        if (keywords.Contains(needle, StringComparison.Ordinal))
        {
            best = Math.Max(best, 380);
        }

        // 3) Sous-séquence floue du titre (les lettres dans l'ordre, pas forcément contiguës).
        if (best == 0 && IsSubsequence(needle, title))
        {
            best = 250;
        }

        // 4) Plein-texte sur le corps (markdown cours/sujet) — faible, mais ajoute un extrait.
        if (command.Body is { Length: > 0 } body)
        {
            // On cherche directement dans le corps ORIGINAL, insensible aux accents et à la casse :
            // l'index renvoyé est ainsi aligné sur le corps original pour découper un extrait correct
            // (Normalize() supprime des caractères → ses index ne correspondent PAS au corps original).
            var bodyIndex = CompareInfo.IndexOf(
                body, needle, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
            if (bodyIndex >= 0)
            {
                best = Math.Max(best, 150);
                snippet = ExtractSnippet(body, bodyIndex);
            }
        }

        if (best == 0)
        {
            return false;
        }

        // Bonus de catégorie (départage à pertinence égale : destinations/actions priment).
        score = best + KindWeight(command.Kind);
        return true;
    }

    private static int KindWeight(SearchKind kind) => kind switch
    {
        SearchKind.Destination => 40,
        SearchKind.Action => 30,
        SearchKind.Module => 20,
        SearchKind.Exercise => 10,
        _ => 0,
    };

    private static bool IsWordStart(string text, int index)
        => index == 0 || !char.IsLetterOrDigit(text[index - 1]);

    /// <summary>Vrai si <paramref name="needle"/> apparaît comme sous-séquence (dans l'ordre) de <paramref name="haystack"/>.</summary>
    private static bool IsSubsequence(string needle, string haystack)
    {
        if (needle.Length == 0)
        {
            return true;
        }

        var n = 0;
        foreach (var c in haystack)
        {
            if (c == needle[n] && ++n == needle.Length)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Extrait un fragment de ~120 caractères autour de la 1ʳᵉ occurrence, espaces normalisés.</summary>
    private static string ExtractSnippet(string body, int matchIndex)
    {
        const int window = 60;
        var start = Math.Max(0, matchIndex - window);
        var end = Math.Min(body.Length, matchIndex + window);
        var fragment = body[start..end];

        var collapsed = CollapseWhitespace(fragment);
        var prefix = start > 0 ? "…" : string.Empty;
        var suffix = end < body.Length ? "…" : string.Empty;
        return prefix + collapsed + suffix;
    }

    private static string CollapseWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalise pour la comparaison : minuscule invariante + suppression des diacritiques
    /// (« Vérifier » → « verifier »), pour que la recherche ignore les accents.
    /// </summary>
    private static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
