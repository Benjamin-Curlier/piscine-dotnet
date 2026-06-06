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
                sources[deliverable] = File.ReadAllText(path);
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
}
