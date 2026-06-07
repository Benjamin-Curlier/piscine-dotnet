using System;
using System.Collections.Generic;

namespace Piscine.Core.Progression;

/// <summary>
/// Résultat d'un grader pour un exercice rendu : type, réussite, et messages verbatim du moteur
/// (le diff « Attendu : … » / « Obtenu  : … » est déjà dans <see cref="Messages"/>).
/// </summary>
public sealed record PushCaseResult(
    string GraderType,
    bool Passed,
    IReadOnlyList<string> Messages);

/// <summary>
/// Résultat riche d'un exercice corrigé lors d'un push : statut (« Reussi »/« ARevoir »/« NonCorrige »,
/// en chaîne pour rester sans dépendance au moteur), cas par grader, indice apparié et renvoi cours.
/// </summary>
public sealed record PushExerciseResult(
    string ExerciseId,
    string ModuleId,
    string Status,
    IReadOnlyList<PushCaseResult> Cases,
    string? Hint,
    string? CourseRef);

/// <summary>
/// Instantané du dernier rendu corrigé par <c>grade-received</c>, persisté à côté de
/// <c>progress.json</c> pour que la page <c>/resultat</c> affiche le diff riche sans re-jouer le grader.
/// </summary>
public sealed record PushResultDocument(
    IReadOnlyList<PushExerciseResult> Exercises,
    DateTimeOffset GradedAt);
