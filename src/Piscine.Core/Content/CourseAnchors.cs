using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Piscine.Core.Content;

/// <summary>
/// Extrait les ancres de titres d'un <c>cours.md</c>, pour valider les <c>course_ref</c>
/// (<c>cours.md#slug</c>) des manifests. Cohérent avec le rendu du site (Markdig
/// <c>UseAdvancedExtensions</c> + <c>UseAutoIdentifiers(GitHub)</c>) : une ancre explicite
/// <c>{#id}</c> en fin de titre prime, sinon on retombe sur un slug façon GitHub du texte du titre.
/// </summary>
public static class CourseAnchors
{
    private static readonly Regex ExplicitId = new(@"\{#(?<id>[^}\s]+)\}\s*$", RegexOptions.Compiled);

    /// <summary>Ancres (slugs) de tous les titres ATX (<c># … ######</c>) du document.</summary>
    public static IReadOnlySet<string> Extract(string courseMarkdown)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(courseMarkdown))
        {
            return anchors;
        }

        foreach (var rawLine in courseMarkdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r', ' ', '\t');
            var hashes = 0;
            while (hashes < line.Length && line[hashes] == '#')
            {
                hashes++;
            }

            // Titre ATX = 1 à 6 '#' suivis d'un espace.
            if (hashes is < 1 or > 6 || hashes >= line.Length || line[hashes] != ' ')
            {
                continue;
            }

            var heading = line[(hashes + 1)..].Trim();
            var explicitMatch = ExplicitId.Match(heading);
            if (explicitMatch.Success)
            {
                anchors.Add(explicitMatch.Groups["id"].Value);
            }
            else if (!string.IsNullOrWhiteSpace(heading))
            {
                anchors.Add(Slugify(heading));
            }
        }

        return anchors;
    }

    /// <summary>
    /// Découpe un <c>course_ref</c> en (fichier, ancre). L'ancre vaut <c>null</c> si la référence
    /// ne contient pas de <c>#</c> (lien vers le document entier).
    /// </summary>
    public static (string File, string? Anchor) ParseRef(string courseRef)
    {
        var hash = courseRef.IndexOf('#');
        return hash < 0
            ? (courseRef.Trim(), null)
            : (courseRef[..hash].Trim(), courseRef[(hash + 1)..].Trim());
    }

    /// <summary>Slug façon GitHub : minuscules, lettres/chiffres conservés, séparateurs → un seul tiret.</summary>
    private static string Slugify(string text)
    {
        var sb = new StringBuilder(text.Length);
        var pendingDash = false;
        foreach (var ch in text.ToLowerInvariant().Normalize(NormalizationForm.FormC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (pendingDash && sb.Length > 0)
                {
                    sb.Append('-');
                }

                pendingDash = false;
                sb.Append(ch);
            }
            else if (ch == '-' || char.IsWhiteSpace(ch) || char.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator)
            {
                pendingDash = true;
            }
            // tout autre caractère (ponctuation) est ignoré
        }

        return sb.ToString();
    }
}
