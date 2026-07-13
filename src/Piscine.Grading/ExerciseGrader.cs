using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Corrige un exercice en dispatchant ses étapes vers les graders enregistrés.</summary>
public sealed class ExerciseGrader
{
    private readonly Dictionary<string, IGrader> _graders = new();

    public ExerciseGrader(IEnumerable<IGrader> graders)
    {
        foreach (var grader in graders)
        {
            _graders[grader.Type] = grader;
        }
    }

    public ExerciseGradingResult Grade(ExerciseManifest manifest, GradingContext context)
    {
        var results = new List<GraderResult>();

        foreach (var step in manifest.Grading)
        {
            if (_graders.TryGetValue(step.Type, out var grader))
            {
                try
                {
                    results.Add(grader.Grade(context, step));
                }
                catch (SandboxUnavailableException ex)
                {
                    // Fail-closed : le bac à sable d'exécution est indisponible (packaging cassé,
                    // binaire absent). On NE retombe PAS en in-process (cela réintroduirait les fuites
                    // et masquerait la casse) et on NE laisse PAS « réussir » : échec interne explicite.
                    // Marqué IsInternalError : affiché à la recrue mais NON persisté comme régression
                    // (une panne transitoire ne doit pas rétrograder un « Réussi » — M-10).
                    results.Add(GraderResult.Internal(step.Type,
                        $"interne : bac à sable d'exécution indisponible — {ex.Message}"));
                }
            }
            else
            {
                // Fail-closed : un type de notation inconnu (typo manifest, grader retiré, étape non
                // encore enregistrée) NE DOIT PAS être ignoré silencieusement — sinon l'exercice
                // « réussit » par défaut pour tout rendu (et validate-content ne l'attrape pas, le
                // corrigé passant aussi). On émet un échec « contenu » explicite.
                results.Add(GraderResult.Failure(step.Type,
                    $"contenu : type de notation inconnu « {step.Type} »."));
            }
        }

        // Une étape de notation absente serait elle aussi un faux « réussi » : on l'interdit.
        if (results.Count == 0)
        {
            results.Add(GraderResult.Failure("contenu",
                "contenu : aucune étape de notation déclarée pour cet exercice."));
        }

        return new ExerciseGradingResult(manifest.Id, results);
    }
}
