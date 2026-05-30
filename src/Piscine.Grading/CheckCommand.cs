using System;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Progression;

namespace Piscine.Grading;

/// <summary>Résultat d'une commande : code de sortie + texte à afficher.</summary>
public sealed record CommandResult(int ExitCode, string Output);

/// <summary>Corrige un exercice localement (boucle <c>check</c>) et persiste la progression.</summary>
public sealed class CheckCommand
{
    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public CheckCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    public CommandResult Run(string exerciseId)
    {
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        if (location is null)
        {
            return new CommandResult(2, $"Exercice introuvable : {exerciseId}");
        }

        var workspaceDir = _layout.WorkspaceExerciseDir(location.ModuleId, exerciseId);
        var submission = SubmissionLoader.Load(location.ContentDir, workspaceDir);

        if (submission.IsEmpty)
        {
            return new CommandResult(1, ResultFormatter.EmptySubmission(exerciseId));
        }

        var result = _grader.Grade(submission.Manifest, submission.Context);

        var store = new ProgressStore(_layout.ProgressPath);
        var progress = store.Load();
        ProgressRecorder.Apply(progress, new[] { result }, DateTimeOffset.Now);
        store.Save(progress);

        var output = ResultFormatter.Format(result, submission.Manifest.Feedback);
        var exitCode = result.Status == GraderStatus.Reussi ? 0 : 1;
        return new CommandResult(exitCode, output);
    }
}
