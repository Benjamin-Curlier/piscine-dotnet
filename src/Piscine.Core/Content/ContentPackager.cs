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
        foreach (var file in Directory.EnumerateFiles(sourceContentDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceContentDir, file);
            if (HasSolutionSegment(relative))
            {
                continue;
            }

            var destination = Path.Combine(destContentDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
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
