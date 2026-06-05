namespace Piscine.Web.Services;

/// <summary>Un exercice prêt à l'affichage (sans corrigé).</summary>
public sealed record CourseExercise(
    string ModuleId,
    string Id,
    string Title,
    string Objective,
    string Difficulty,
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
    string CourseMarkdown,
    IReadOnlyList<CourseGroup> Groups)
{
    /// <summary>Numéro affiché, déduit du préfixe de l'identifiant (ex. <c>01-bases-csharp</c> → <c>01</c>).</summary>
    public string Number => Id.Split('-', 2)[0];

    public int ExerciseCount => Groups.Sum(g => g.Exercises.Count);

    public bool HasExercises => ExerciseCount > 0;
}
