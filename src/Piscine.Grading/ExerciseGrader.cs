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

    public ExerciseGradingResult Grade(ExerciseManifest manifest, IReadOnlyDictionary<string, string> sources)
    {
        var results = new List<GraderResult>();

        foreach (var step in manifest.Grading)
        {
            if (_graders.TryGetValue(step.Type, out var grader))
            {
                results.Add(grader.Grade(sources, step));
            }
        }

        return new ExerciseGradingResult(manifest.Id, results);
    }
}
