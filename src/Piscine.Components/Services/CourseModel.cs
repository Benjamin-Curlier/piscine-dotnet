namespace Piscine.Components.Services;

/// <summary>Un exercice prêt à l'affichage (sans corrigé).</summary>
public sealed record CourseExercise(
    string ModuleId,
    string Id,
    string Title,
    string Objective,
    string Difficulty,
    int EstimatedMinutes,
    int Xp,
    IReadOnlyList<string> Tags,
    string StoryBeat,
    bool Bonus,
    IReadOnlyList<string> Deliverables,
    string? SubjectMarkdown);

/// <summary>Un groupe d'exercices au sein d'un module.</summary>
public sealed record CourseGroup(
    string Id,
    string Title,
    IReadOnlyList<CourseExercise> Exercises);

/// <summary>Un module pédagogique prêt à l'affichage : cours + groupes d'exercices.</summary>
public sealed record CourseModule(
    string Id,
    int Order,
    string Title,
    IReadOnlyList<string> PrerequisiteIds,
    string Arc,
    string Mission,
    string CourseMarkdown,
    IReadOnlyList<CourseGroup> Groups)
{
    /// <summary>Numéro affiché, déduit du préfixe de l'identifiant (ex. <c>01-bases-csharp</c> → <c>01</c>).</summary>
    public string Number => Id.Split('-', 2)[0];

    public int ExerciseCount => Groups.Sum(g => g.Exercises.Count);

    public bool HasExercises => ExerciseCount > 0;

    public int EstimatedMinutes => Groups.SelectMany(group => group.Exercises).Sum(exercise => exercise.EstimatedMinutes);

    public int TotalXp => Groups.SelectMany(group => group.Exercises).Sum(exercise => exercise.Xp);
}

/// <summary>Mini-projet transverse affiché dans l'application au même titre que les modules.</summary>
public sealed record CourseRush(
    string Id,
    string Title,
    string Objective,
    string Difficulty,
    int EstimatedMinutes,
    int Xp,
    IReadOnlyList<string> Tags,
    string StoryBeat,
    string RecommendedAfterModuleId,
    bool ManualValidation,
    IReadOnlyList<string> GradingTypes,
    bool Bonus,
    IReadOnlyList<string> Deliverables,
    string? SubjectMarkdown);
