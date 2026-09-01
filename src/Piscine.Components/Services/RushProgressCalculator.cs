using Piscine.App.Board;
using Piscine.App.Progress;
using Piscine.Core.Content;

namespace Piscine.Components.Services;

/// <summary>État d'une mission de synthèse, enrichi de son jalon et de ses tentatives.</summary>
public sealed record RushProgressItem(
    CourseRush Rush,
    ExerciseStatusInfo StatusInfo,
    bool MilestoneReached,
    int Attempts,
    DateTimeOffset? LastAttempt)
{
    public bool IsComplete => StatusInfo.Status == ExerciseProgressStatus.PousseNote;

    public bool IsStarted => StatusInfo.Status is
        ExerciseProgressStatus.EnCours or
        ExerciseProgressStatus.CommiteNonPousse or
        ExerciseProgressStatus.EnAttenteRelecture or
        ExerciseProgressStatus.ARevoir;

    public string ActionLabel => StatusInfo.Status switch
    {
        ExerciseProgressStatus.NonCommence => MilestoneReached ? "Démarrer" : "Voir la mission",
        ExerciseProgressStatus.PousseNote => "Revoir",
        _ => "Reprendre",
    };
}

/// <summary>
/// Construit la progression des Rushes depuis un instantané unique. Les jalons restent des conseils :
/// une mission non encore recommandée demeure toujours accessible.
/// </summary>
public static class RushProgressCalculator
{
    public static IReadOnlyList<RushProgressItem> Build(
        CourseCatalog catalog,
        IReadOnlyList<ExerciseStatusInfo> statuses,
        Piscine.Core.Model.Progress persistedProgress)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(statuses);
        ArgumentNullException.ThrowIfNull(persistedProgress);

        var statusIndex = statuses.ToDictionary(
            status => (status.ModuleId, status.ExerciseId),
            status => status);

        return catalog.Rushes.Select(rush =>
        {
            var key = (ContentLocator.RushesModuleId, rush.Id);
            var status = statusIndex.GetValueOrDefault(key)
                ?? new ExerciseStatusInfo(
                    ContentLocator.RushesModuleId,
                    rush.Id,
                    ExerciseProgressStatus.NonCommence,
                    StatusSource.Progress);

            var milestone = catalog.GetRecommendedModule(rush);
            var milestoneReached = milestone is not null && catalog.Modules
                .Where(module => module.Order <= milestone.Order)
                .SelectMany(module => module.Groups.SelectMany(group => group.Exercises))
                .All(exercise => statusIndex.GetValueOrDefault((exercise.ModuleId, exercise.Id))?.Status
                    == ExerciseProgressStatus.PousseNote);

            persistedProgress.Exercises.TryGetValue(rush.Id, out var entry);
            return new RushProgressItem(
                rush,
                status,
                milestoneReached,
                entry?.Attempts ?? 0,
                entry?.LastAttempt);
        }).ToList();
    }

    /// <summary>
    /// Priorité à une mission à revoir, puis déjà commencée, puis disponible. À défaut, montre le
    /// prochain jalon pour rendre la suite du parcours visible sans la présenter comme débloquée.
    /// </summary>
    public static RushProgressItem? Recommend(IReadOnlyList<RushProgressItem> rushes)
        => rushes.FirstOrDefault(item => item.StatusInfo.Status == ExerciseProgressStatus.ARevoir)
           ?? rushes.FirstOrDefault(item => item.StatusInfo.Status == ExerciseProgressStatus.EnAttenteRelecture)
           ?? rushes.FirstOrDefault(item => item.IsStarted)
           ?? rushes.FirstOrDefault(item => item.MilestoneReached && !item.IsComplete)
           ?? rushes.FirstOrDefault(item => !item.IsComplete);

    public static BoardCounts Counts(IReadOnlyList<RushProgressItem> rushes)
        => BoardCounts.From(rushes.Select(item => item.StatusInfo.Status).ToList());
}
