using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Grading;

namespace Piscine.Git;

/// <summary>
/// Rendu officiel : corrige le commit reçu par push, par groupe et dans l'ordre
/// (stop au 1er KO), puis persiste la progression. Réutilise tout le moteur de notation.
/// </summary>
public sealed class GradeReceivedCommand
{
    /// <summary>Branche de rendu : la recrue pousse <c>origin main</c>. Sert de « HEAD » au grader git
    /// côté dépôt bare (dont le HEAD réel reste orphelin après un push).</summary>
    private const string RenduBranch = "main";

    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public GradeReceivedCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    public CommandResult Run(string sha)
    {
        var snapshot = Path.Combine(Path.GetTempPath(), "piscine-recu", Guid.NewGuid().ToString("N"));
        try
        {
            CommitExtractor.Extract(_layout.RemoteRepoPath, sha, snapshot);

            var allResults = new List<ExerciseGradingResult>();
            var groupGrader = new GroupGrader(_grader);

            foreach (var module in ContentDiscovery.DiscoverModules(_layout.Content))
            {
                foreach (var group in module.Groups)
                {
                    var submissions = new List<ExerciseSubmission>();
                    foreach (var exerciseId in group.Exercises)
                    {
                        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
                        if (location is null)
                        {
                            continue;
                        }

                        var manifest = ExerciseManifestLoader.Load(location.ContentDir);
                        var gitStep = manifest.Grading.FirstOrDefault(s => s.Type == "git");
                        if (gitStep is not null)
                        {
                            // Exo git : pas de fichier dans le snapshot plat — noté contre le dépôt bare
                            // (qui contient les refs poussées), et seulement si « tenté » (cf. #17), pour
                            // éviter un « à revoir » parasite sur un exo non commencé.
                            if (GitAttemptEvaluator.IsAttempted(gitStep.Git?.Attempt, _layout.RemoteRepoPath))
                            {
                                submissions.Add(new ExerciseSubmission(
                                    manifest,
                                    new GradingContext(
                                        new Dictionary<string, string>(),
                                        repositoryPath: _layout.RemoteRepoPath,
                                        headRef: RenduBranch)));
                            }

                            continue; // un exo git ne se charge jamais depuis le snapshot plat
                        }

                        var submittedDir = Path.Combine(snapshot, module.Id, exerciseId);
                        if (!Directory.Exists(submittedDir))
                        {
                            continue; // exercice non rendu dans ce push
                        }

                        var submission = SubmissionLoader.Load(location.ContentDir, submittedDir);
                        if (submission.IsEmpty)
                        {
                            continue; // dossier présent mais aucun livrable : exercice non rendu
                        }

                        submissions.Add(submission);
                    }

                    if (submissions.Count > 0)
                    {
                        allResults.AddRange(groupGrader.GradeGroup(submissions));
                    }
                }
            }

            // Rushes : projets autonomes, notés indépendamment (pas de stop-au-1er-KO de groupe).
            foreach (var rush in ContentDiscovery.DiscoverRushes(_layout.Content))
            {
                var location = ContentLocator.FindExercise(_layout.Content, rush.Id);
                if (location is null)
                {
                    continue;
                }

                var submittedDir = Path.Combine(snapshot, ContentLocator.RushesModuleId, rush.Id);
                if (!Directory.Exists(submittedDir))
                {
                    continue;
                }

                var submission = SubmissionLoader.Load(location.ContentDir, submittedDir);
                if (submission.IsEmpty)
                {
                    continue;
                }

                allResults.AddRange(groupGrader.GradeGroup(new List<ExerciseSubmission> { submission }));
            }

            return Persist(allResults);
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    private CommandResult Persist(IReadOnlyList<ExerciseGradingResult> results)
    {
        if (results.Count == 0)
        {
            return new CommandResult(0, "Aucun exercice reconnu dans ce rendu.");
        }

        var store = new ProgressStore(_layout.ProgressPath);
        var progress = store.Load();
        ProgressRecorder.Apply(progress, results, DateTimeOffset.Now);
        store.Save(progress);

        // En plus du statut (progress.json), persiste le verdict RICHE (diff/indice/cours) du push
        // pour que /resultat l'affiche sans re-jouer le grader (#40). Best-effort : ne casse pas le
        // rendu si l'écriture échoue.
        PersistRichResult(results);

        var sb = new StringBuilder();
        var anyToReview = false;
        foreach (var result in results)
        {
            sb.AppendLine(ResultFormatter.Format(result, FeedbackFor(result.ExerciseId)));
            if (result.Status == GraderStatus.ARevoir)
            {
                anyToReview = true;
            }
        }

        return new CommandResult(anyToReview ? 1 : 0, sb.ToString().TrimEnd());
    }

    /// <summary>
    /// Écrit le résultat riche du push (par exercice : statut, cas/diff verbatim, indice apparié,
    /// renvoi cours) dans <c>last-push-result.json</c>. Résolution indice/cours identique à
    /// <c>ResultFormatter</c> / <c>CheckService</c>. Best-effort : une erreur d'écriture est ignorée.
    /// </summary>
    private void PersistRichResult(IReadOnlyList<ExerciseGradingResult> results)
    {
        var exercises = new List<PushExerciseResult>(results.Count);
        foreach (var result in results)
        {
            var location = ContentLocator.FindExercise(_layout.Content, result.ExerciseId);
            var feedback = location is null
                ? new FeedbackConfig()
                : ExerciseManifestLoader.Load(location.ContentDir).Feedback;

            var cases = result.Results
                .Select(r => new PushCaseResult(r.GraderType, r.Status == GraderStatus.Reussi, r.Messages))
                .ToList();

            string? hint = null;
            string? courseRef = null;
            if (result.Status == GraderStatus.ARevoir)
            {
                var trigger = result.Results
                    .FirstOrDefault(r => r.Status == GraderStatus.ARevoir && r.Trigger is not null)
                    ?.Trigger;
                if (trigger is not null)
                {
                    hint = feedback.Hints.FirstOrDefault(h => h.When == trigger)?.Message;
                }

                courseRef = string.IsNullOrWhiteSpace(feedback.CourseRef) ? null : feedback.CourseRef;
            }

            exercises.Add(new PushExerciseResult(
                result.ExerciseId,
                location?.ModuleId ?? string.Empty,
                result.Status.ToString(),
                cases,
                hint,
                courseRef));
        }

        try
        {
            new LastPushResultStore(_layout.LastPushResultPath)
                .Save(new PushResultDocument(exercises, DateTimeOffset.Now));
        }
        catch (IOException)
        {
            // Best-effort : l'absence de l'artefact riche fait retomber /resultat sur le statut seul.
        }
    }

    private FeedbackConfig FeedbackFor(string exerciseId)
    {
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        return location is null
            ? new FeedbackConfig()
            : ExerciseManifestLoader.Load(location.ContentDir).Feedback;
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
