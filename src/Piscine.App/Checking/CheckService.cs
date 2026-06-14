using System.Collections.Generic;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Grading;

namespace Piscine.App.Checking;

/// <summary>
/// Corrige un exercice in-process (via <c>Piscine.Grading</c>) et renvoie un résultat structuré UI.
/// Pur et déterministe : sans console, sans git, sans persistance de progression.
/// </summary>
public sealed class CheckService
{
    // Le moteur redirige Console.Out (process-global) pendant l'exécution in-process : deux
    // corrections simultanées (plusieurs circuits / double-clic) corromperaient la capture stdout.
    // La correction étant CPU-bound et de fait séquentielle, on sérialise les passages ici (process-wide).
    private static readonly object GradeGate = new();

    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public CheckService(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    /// <summary>Localise, charge et corrige <paramref name="exerciseId"/> in-process.</summary>
    public CheckOutcome Check(string exerciseId)
    {
        // 1. Localiser l'exercice dans le contenu
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        if (location is null)
        {
            return new CheckOutcome(exerciseId, string.Empty, CheckVerdict.Introuvable, [], null, null);
        }

        // 2. Charger la soumission depuis le workspace
        var workspaceDir = _layout.WorkspaceExerciseDir(location.ModuleId, exerciseId);
        var submission = SubmissionLoader.Load(location.ContentDir, workspaceDir);

        // 3. Soumission vide → aucun fichier rendu
        if (submission.IsEmpty)
        {
            return new CheckOutcome(exerciseId, location.ModuleId, CheckVerdict.AucunFichier, [], null, null);
        }

        // 4. Corriger in-process (compilation + exécution délégués au moteur). Sérialisé : cf. GradeGate.
        ExerciseGradingResult result;
        lock (GradeGate)
        {
            result = _grader.Grade(submission.Manifest, submission.Context);
        }

        // 5. Mapper les résultats grader → CheckCaseResult, en dérivant le diff structuré
        //    (couche App, sans toucher au grader) depuis les messages « Attendu/Obtenu ».
        var cases = result.Results
            .Select(static r => new CheckCaseResult(
                r.GraderType,
                r.Status == GraderStatus.Reussi,
                r.Messages,
                StructuredDiffBuilder.TryBuild(r.Messages)))
            .ToList();

        // 6. Résoudre l'indice + course_ref exactement comme ResultFormatter.MatchHint
        string? hint = null;
        string? courseRef = null;

        if (result.Status == GraderStatus.ARevoir)
        {
            var trigger = result.Results
                .FirstOrDefault(static r => r.Status == GraderStatus.ARevoir && r.Trigger is not null)
                ?.Trigger;

            if (trigger is not null)
            {
                hint = submission.Manifest.Feedback.Hints
                    .FirstOrDefault(h => h.When == trigger)
                    ?.Message;
            }

            courseRef = string.IsNullOrWhiteSpace(submission.Manifest.Feedback.CourseRef)
                ? null
                : submission.Manifest.Feedback.CourseRef;
        }

        // 7. Mapper le verdict (NonCorrige traité comme ARevoir côté UI : cas improbable sans groupe)
        var verdict = result.Status switch
        {
            GraderStatus.Reussi => CheckVerdict.Reussi,
            _ => CheckVerdict.ARevoir,
        };

        return new CheckOutcome(exerciseId, location.ModuleId, verdict, cases, hint, courseRef);
    }
}
