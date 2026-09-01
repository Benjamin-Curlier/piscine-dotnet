using System.Collections.Generic;
using System.IO;
using Piscine.Core.Content;

namespace Piscine.Grading;

/// <summary>Assemble une <see cref="ExerciseSubmission"/> depuis le disque.</summary>
public static class SubmissionLoader
{
    public static ExerciseSubmission Load(string exerciseContentDir, string workspaceExerciseDir)
    {
        var manifest = ExerciseManifestLoader.Load(exerciseContentDir);

        var sources = new Dictionary<string, string>();
        foreach (var deliverable in manifest.Deliverables)
        {
            var path = Path.Combine(workspaceExerciseDir, deliverable);
            if (File.Exists(path))
            {
                sources[deliverable] = RemoveFileBasedDirectives(File.ReadAllText(path));
            }
        }

        var graderFiles = new Dictionary<string, string>();
        foreach (var step in manifest.Grading)
        {
            foreach (var testFile in step.TestFiles)
            {
                var path = Path.Combine(exerciseContentDir, testFile);
                if (File.Exists(path))
                {
                    graderFiles[testFile] = File.ReadAllText(path);
                }
            }

            if (!string.IsNullOrEmpty(step.Reference))
            {
                var referencePath = Path.Combine(exerciseContentDir, step.Reference);
                if (File.Exists(referencePath))
                {
                    graderFiles[step.Reference] = File.ReadAllText(referencePath);
                }
            }
        }

        // Le dossier rendu peut être un dépôt git (grader `git`) : on le transmet tel quel.
        return new ExerciseSubmission(manifest, new GradingContext(sources, graderFiles, workspaceExerciseDir));
    }

    /// <summary>
    /// Les directives <c>#!</c> et <c>#:</c> appartiennent au mode « fichier C# » de <c>dotnet run</c>
    /// et de Rider, pas à la syntaxe compilée directement par Roslyn. La Piscine fournit déjà les
    /// références et assemble déjà tous les livrables : on retire donc ces directives en conservant
    /// une ligne vide, afin que les numéros de ligne des diagnostics restent exacts.
    /// </summary>
    internal static string RemoveFileBasedDirectives(string source)
    {
        var lines = source.Split('\n');
        var changed = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var hasCarriageReturn = lines[i].EndsWith('\r');
            var content = hasCarriageReturn ? lines[i][..^1] : lines[i];
            var trimmed = content.TrimStart();
            if (!trimmed.StartsWith("#!", StringComparison.Ordinal)
                && !trimmed.StartsWith("#:", StringComparison.Ordinal))
            {
                continue;
            }

            // Préserve le séparateur CRLF de la ligne et la présence/absence du saut final.
            lines[i] = hasCarriageReturn ? "\r" : string.Empty;
            changed = true;
        }

        return changed ? string.Join('\n', lines) : source;
    }
}
