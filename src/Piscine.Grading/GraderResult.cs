using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat éducatif produit par un grader pour une étape de notation.</summary>
public sealed class GraderResult
{
    private GraderResult(
        string graderType, GraderStatus status, IReadOnlyList<string> messages, string? trigger, bool isInternalError = false)
    {
        GraderType = graderType;
        Status = status;
        Messages = messages;
        Trigger = trigger;
        IsInternalError = isInternalError;
    }

    public string GraderType { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<string> Messages { get; }

    /// <summary>
    /// Vrai si l'échec provient de l'INFRASTRUCTURE (bac à sable indisponible…), pas de la recrue. Affiché
    /// mais NON persisté comme régression : une panne transitoire ne doit pas rétrograder un « Réussi » (M-10).
    /// </summary>
    public bool IsInternalError { get; }

    /// <summary>
    /// Déclencheur de feedback en cas d'échec (cf. <see cref="Piscine.Core.Model.FeedbackTriggers"/>),
    /// ou <c>null</c>. Permet au formateur d'afficher le hint dont le <c>when</c> correspond.
    /// </summary>
    public string? Trigger { get; }

    public static GraderResult Success(string graderType) =>
        new(graderType, GraderStatus.Reussi, new List<string>(), null);

    public static GraderResult Failure(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.ARevoir, messages.ToList(), null);

    /// <summary>Échec d'infrastructure (fail-closed) : affiché, mais qui ne doit pas rétrograder la progression.</summary>
    public static GraderResult Internal(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.ARevoir, messages.ToList(), null, isInternalError: true);

    /// <summary>Réussite avec messages consultatifs (ex. norme non bloquante).</summary>
    public static GraderResult Advisory(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.Reussi, messages.ToList(), null);

    /// <summary>Copie du résultat en y attachant un déclencheur de feedback.</summary>
    public GraderResult WithTrigger(string trigger) =>
        new(GraderType, Status, Messages, trigger, IsInternalError);
}
