using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat éducatif produit par un grader pour une étape de notation.</summary>
public sealed class GraderResult
{
    private GraderResult(string graderType, GraderStatus status, IReadOnlyList<string> messages)
    {
        GraderType = graderType;
        Status = status;
        Messages = messages;
    }

    public string GraderType { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<string> Messages { get; }

    public static GraderResult Success(string graderType) =>
        new(graderType, GraderStatus.Reussi, new List<string>());

    public static GraderResult Failure(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.ARevoir, messages.ToList());

    /// <summary>Réussite avec messages consultatifs (ex. norme non bloquante).</summary>
    public static GraderResult Advisory(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.Reussi, messages.ToList());
}
