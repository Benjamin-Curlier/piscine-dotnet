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

    // Cache (emplacement, manifest) par exercice, vidé à chaque Run : sans lui, chaque exercice était
    // re-résolu et re-parsé jusqu'à 3× par push (Run + PersistRichResult + FeedbackFor).
    private readonly Dictionary<string, (ExerciseLocation Location, ExerciseManifest Manifest)?> _resolved =
        new(StringComparer.Ordinal);

    public GradeReceivedCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    /// <summary>Résout l'emplacement + le manifest d'un exercice, en mémoïsant le résultat (y compris l'absence).</summary>
    private (ExerciseLocation Location, ExerciseManifest Manifest)? Resolve(string exerciseId)
    {
        if (_resolved.TryGetValue(exerciseId, out var cached))
        {
            return cached;
        }

        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        (ExerciseLocation Location, ExerciseManifest Manifest)? entry =
            location is null ? null : (location, ExerciseManifestLoader.Load(location.ContentDir));
        _resolved[exerciseId] = entry;
        return entry;
    }

    /// <summary>Vrai pour le SHA « tout-zéro » (suppression de ref), quel que soit l'algo (40 ou 64 chars).</summary>
    private static bool IsZeroSha(string sha)
    {
        foreach (var c in sha)
        {
            if (c != '0')
            {
                return false;
            }
        }

        return true;
    }

    public CommandResult Run(string sha)
    {
        // Le hook post-receive appelle `grade-received <newrev>` pour CHAQUE ref reçue. Une suppression
        // de branche (ou toute ref dont la nouvelle valeur est le SHA tout-zéro) n'a pas de commit à
        // corriger : on sort proprement (exit 0) au lieu de laisser CommitExtractor lever une exception
        // non gérée qui s'afficherait dans la sortie du `git push` de la recrue.
        if (string.IsNullOrWhiteSpace(sha) || IsZeroSha(sha))
        {
            return new CommandResult(0, "Rien à corriger (référence supprimée ou vide).");
        }

        var snapshot = Path.Combine(Path.GetTempPath(), "piscine-recu", Guid.NewGuid().ToString("N"));
        _resolved.Clear(); // cache (emplacement, manifest) scopé à ce Run.
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
                        var resolved = Resolve(exerciseId);
                        if (resolved is null)
                        {
                            continue;
                        }

                        var (location, manifest) = resolved.Value;
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
                var resolved = Resolve(rush.Id);
                if (resolved is null)
                {
                    continue;
                }

                var location = resolved.Value.Location;
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
            var resolved = Resolve(result.ExerciseId);
            var feedback = resolved?.Manifest.Feedback ?? new FeedbackConfig();

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
                resolved?.Location.ModuleId ?? string.Empty,
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
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best-effort : l'absence de l'artefact riche fait retomber /resultat sur le statut seul.
            // UnauthorizedAccessException incluse : LastPushResultStore.Save peut la lever (chemin non
            // inscriptible) — ne jamais casser le hook après que progress.json a déjà été commité.
        }
    }

    private FeedbackConfig FeedbackFor(string exerciseId)
        => Resolve(exerciseId)?.Manifest.Feedback ?? new FeedbackConfig();

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
