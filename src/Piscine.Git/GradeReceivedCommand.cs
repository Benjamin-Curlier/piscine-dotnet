using System;
using System.Collections.Generic;
using System.IO;
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
