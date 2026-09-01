using Piscine.Core;
using Piscine.Core.Progression;

namespace Piscine.Grading;

/// <summary>Rejoue sur le code courant l'exercice du dernier <c>check</c> en échec.</summary>
public sealed class ReplayCheckCommand
{
    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public ReplayCheckCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    public CommandResult Run()
    {
        var failure = new LastCheckFailureStore(_layout.LastCheckFailurePath).Load();
        if (failure is null)
        {
            return new CommandResult(2, "Aucun échec local enregistré. Lance d'abord piscine check <exo>.");
        }

        var replay = new CheckCommand(_layout, _grader).Run(failure.ExerciseId);
        var heading = $"Rejeu de {failure.ExerciseId} (échec enregistré le {failure.RecordedAt:yyyy-MM-dd HH:mm:ss zzz})";
        return replay with { Output = heading + System.Environment.NewLine + replay.Output };
    }
}
