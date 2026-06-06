using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>
/// Déclencheurs de feedback connus : valeurs autorisées pour <see cref="FeedbackHint.When"/>.
/// Un grader renseigne l'un d'eux en cas d'échec ; le formateur affiche alors le hint
/// dont le <c>when</c> correspond.
/// </summary>
public static class FeedbackTriggers
{
    /// <summary>Le code de la recrue ne compile pas.</summary>
    public const string CompileError = "compile_error";

    /// <summary>La sortie standard diffère de l'attendu (grader <c>io</c>).</summary>
    public const string IoMismatch = "io_mismatch";

    /// <summary>Le code de sortie diffère de l'attendu (grader <c>io</c>).</summary>
    public const string ExitCode = "exit_code";

    /// <summary>Le programme ne s'est pas terminé à temps (boucle infinie ?).</summary>
    public const string Timeout = "timeout";

    /// <summary>Le programme a levé une exception à l'exécution.</summary>
    public const string RuntimeError = "runtime_error";

    /// <summary>Un ou plusieurs tests <c>[Fact]</c> ont échoué (grader <c>unit</c>).</summary>
    public const string UnitFailure = "unit_failure";

    /// <summary>Les tests de la recrue échouent sur l'implémentation correcte (grader <c>mutation</c>).</summary>
    public const string TestsFailOnReference = "tests_fail_on_reference";

    /// <summary>Un mutant a survécu : un comportement bogué n'est pas détecté (grader <c>mutation</c>).</summary>
    public const string MutantSurvived = "mutant_survived";

    /// <summary>Le formatage diffère de la norme (grader <c>norme</c> bloquant).</summary>
    public const string NormeViolation = "norme_violation";

    /// <summary>Ensemble des déclencheurs reconnus (pour validation du contenu).</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        CompileError, IoMismatch, ExitCode, Timeout, RuntimeError, UnitFailure, NormeViolation,
        TestsFailOnReference, MutantSurvived,
    };
}
