namespace Piscine.App.Progress;

/// <summary>
/// Statut de progression d'un exercice, pur et sans note. Construit par
/// <see cref="ProgressService"/> ; consommé par les composants UI.
/// </summary>
public sealed record ExerciseStatusInfo(
    string ModuleId,
    string ExerciseId,
    ExerciseProgressStatus Status,
    StatusSource Source);
