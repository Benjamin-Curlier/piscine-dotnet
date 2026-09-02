using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;
using Piscine.Core.Progression;

namespace Piscine.Grading;

/// <summary>
/// Enregistre l'auto-relecture locale d'une mission dont les contrôles automatiques passent.
/// L'apprenant reste libre et responsable de la sincérité de son attestation.
/// </summary>
public sealed class ManualReviewCommand
{
    private readonly PiscineLayout _layout;
    private readonly TimeProvider _timeProvider;

    public ManualReviewCommand(PiscineLayout layout, TimeProvider? timeProvider = null)
    {
        _layout = layout;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public CommandResult Run(
        string exerciseId,
        bool approved,
        string evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return new CommandResult(64, "La référence des preuves relues est obligatoire.");
        }

        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        if (location is null)
        {
            return new CommandResult(2, $"Mission introuvable : {exerciseId}");
        }

        var manifest = ExerciseManifestLoader.Load(location.ContentDir);
        if (!manifest.ManualValidation)
        {
            return new CommandResult(2, $"La mission {exerciseId} ne demande pas d'auto-relecture guidée.");
        }

        var store = new ProgressStore(_layout.ProgressPath);
        return store.Update(progress =>
        {
            if (!progress.Exercises.TryGetValue(exerciseId, out var entry)
                || (approved && entry.Status != ExerciseStatus.EnAttenteRelecture)
                || (!approved && entry.Status is not (ExerciseStatus.EnAttenteRelecture or ExerciseStatus.Reussi)))
            {
                return new CommandResult(
                    1,
                    approved
                        ? $"La mission {exerciseId} n'est pas en attente de relecture. Lance d'abord ses contrôles automatiques."
                        : $"La mission {exerciseId} n'a pas d'auto-relecture à rouvrir.");
            }

            var now = _timeProvider.GetLocalNow();
            entry.Status = approved ? ExerciseStatus.Reussi : ExerciseStatus.ARevoir;
            entry.ReviewedAt = now;
            entry.ReviewedBy = "auto-relecture";
            entry.ReviewEvidence = evidence.Trim();
            if (approved)
            {
                entry.FirstSucceededAt ??= now;
            }

            var verdict = approved ? "validée après auto-relecture" : "rouverte pour amélioration";
            return new CommandResult(
                0,
                $"Mission {exerciseId} {verdict}.\n" +
                $"Preuves : {entry.ReviewEvidence}\n" +
                "Attestation personnelle locale : elle sert ta progression et ne constitue pas une certification externe.");
        });
    }
}
