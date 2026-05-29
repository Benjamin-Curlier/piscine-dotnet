using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>
/// Corrige un groupe d'exercices dans l'ordre et s'arrête au premier échec :
/// les exercices suivants sont marqués <see cref="GraderStatus.NonCorrige"/>.
/// </summary>
public sealed class GroupGrader
{
    private readonly ExerciseGrader _grader;

    public GroupGrader(ExerciseGrader grader)
    {
        _grader = grader;
    }

    public IReadOnlyList<ExerciseGradingResult> GradeGroup(IEnumerable<ExerciseSubmission> submissions)
    {
        var results = new List<ExerciseGradingResult>();
        var stopped = false;

        foreach (var submission in submissions)
        {
            if (stopped)
            {
                results.Add(ExerciseGradingResult.NotGraded(submission.Manifest.Id));
                continue;
            }

            var result = _grader.Grade(submission.Manifest, submission.Context);
            results.Add(result);

            if (result.Status == GraderStatus.ARevoir)
            {
                stopped = true;
            }
        }

        return results;
    }
}
