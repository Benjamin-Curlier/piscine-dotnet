using System.Collections.Generic;

namespace Piscine.App.Checking;

/// <summary>Verdict global d'un check in-process (jamais de note ni de score).</summary>
public enum CheckVerdict
{
    Reussi,
    ARevoir,
    AucunFichier,
    Introuvable,
}

/// <summary>
/// Résultat d'un grader pour un exercice : type, réussite et messages verbatim du moteur.
/// Le diff attendu/obtenu est déjà présent dans <see cref="Messages"/> (ligne « Attendu : … »
/// / « Obtenu  : … ») tel que produit par <c>IoGrader</c>, retours à la ligne échappés en <c>\n</c>.
/// </summary>
public sealed record CheckCaseResult(
    string GraderType,
    bool Passed,
    IReadOnlyList<string> Messages);

/// <summary>
/// Résultat structuré d'un <see cref="CheckService.Check"/> : verdict + cas + indice + cours.
/// Pur, immuable, sans console ni git ni persistance.
/// </summary>
public sealed record CheckOutcome(
    string ExerciseId,
    string ModuleId,
    CheckVerdict Verdict,
    IReadOnlyList<CheckCaseResult> Cases,
    string? Hint,
    string? CourseRef);
