using System;
using System.IO;

namespace Piscine.Core.Content;

/// <summary>
/// Copie une arborescence de contenu vers une destination en EXCLUANT tout dossier
/// <c>solution/</c> (les corrigés de référence ne sont jamais distribués). (spec §3.3)
/// </summary>
/// <remarks>
/// CONTRAT (ne pas « durcir » naïvement) : <c>solution/</c> est le SEUL dossier exclu car c'est le seul
/// corrigé pur dont la correction locale n'a JAMAIS besoin (il ne sert qu'à <c>validate-content</c> en CI).
/// Les autres entrées de grader — notamment <c>reference/</c> (impl. de référence du grader mutation) et
/// d'éventuels tests cachés — DOIVENT être empaquetées : la moulinette tourne 100 % en local (cf.
/// <c>MutationGrader</c> qui lit <c>reference/</c> via <c>GradingContext.GraderFiles</c> au moment de la
/// correction). Les exclure casserait la correction hors-ligne. C'est le même compromis inhérent que
/// <c>expect_stdout</c> présent dans le <c>manifest.yaml</c> distribué : avec une correction locale, toute
/// entrée de grader est nécessairement lisible sur le poste. Passer en liste blanche « anti-fuite »
/// reviendrait à retirer <c>reference/</c> et à casser le module 13 (mutation).
/// </remarks>
public static class ContentPackager
{
    public const string SolutionDirName = "solution";

    public static void CopyWithoutSolutions(string sourceContentDir, string destContentDir)
    {
        var source = Path.GetFullPath(sourceContentDir);
        var destinationRoot = Path.GetFullPath(destContentDir);
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Contenu source introuvable : {source}");
        }

        // Une destination imbriquée serait ré-énumérée pendant la copie ; une source imbriquée dans
        // la destination serait détruite au nettoyage. Le packaging exige donc deux arbres disjoints.
        if (IsSameOrDescendant(source, destinationRoot)
            || IsSameOrDescendant(destinationRoot, source))
        {
            throw new ArgumentException("Les dossiers source et destination du paquet doivent être disjoints.");
        }

        // Le paquet est un instantané, pas une copie incrémentale : des fichiers provenant d'une
        // exécution précédente (notamment un ancien solution/) ne doivent jamais survivre.
        if (Directory.Exists(destinationRoot))
        {
            Directory.Delete(destinationRoot, recursive: true);
        }
        Directory.CreateDirectory(destinationRoot);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (HasSolutionSegment(relative))
            {
                continue;
            }

            var destination = Path.Combine(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static bool IsSameOrDescendant(string candidate, string parent)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(candidate, parent, comparison))
        {
            return true;
        }

        var prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, comparison);
    }

    private static bool HasSolutionSegment(string relativePath)
    {
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Equals(SolutionDirName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
