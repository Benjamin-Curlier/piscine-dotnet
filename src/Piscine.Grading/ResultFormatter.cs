using System.Linq;
using System.Text;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Rend un résultat de correction en texte éducatif pour la console.</summary>
public static class ResultFormatter
{
    public static string Format(ExerciseGradingResult result, FeedbackConfig feedback)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {result.ExerciseId} : {Label(result.Status)} ===");

        foreach (var graderResult in result.Results)
        {
            foreach (var message in graderResult.Messages)
            {
                sb.AppendLine($"[{graderResult.GraderType}] {message}");
            }
        }

        if (result.Status == GraderStatus.ARevoir)
        {
            var hint = MatchHint(result, feedback);
            if (hint is not null)
            {
                sb.AppendLine($"→ Indice : {hint}");
            }

            if (!string.IsNullOrWhiteSpace(feedback.CourseRef))
            {
                sb.AppendLine($"→ Revois le cours : {feedback.CourseRef}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Message éducatif pour une soumission vide : oriente la recrue vers le démarrage de l'exercice
    /// au lieu d'afficher une erreur de compilation cryptique.
    /// </summary>
    public static string EmptySubmission(string exerciseId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {exerciseId} : Aucun fichier rendu ===");
        sb.AppendLine($"Aucun fichier rendu pour {exerciseId}.");
        sb.AppendLine($"Commence par : piscine start {exerciseId}, puis code dans le workspace.");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Retourne le message du hint dont le <c>when</c> correspond au déclencheur de l'échec, ou <c>null</c>.</summary>
    private static string? MatchHint(ExerciseGradingResult result, FeedbackConfig feedback)
    {
        if (feedback.Hints.Count == 0)
        {
            return null;
        }

        var trigger = result.Results
            .FirstOrDefault(r => r.Status == GraderStatus.ARevoir && r.Trigger is not null)?
            .Trigger;
        if (trigger is null)
        {
            return null;
        }

        return feedback.Hints.FirstOrDefault(h => h.When == trigger)?.Message;
    }

    private static string Label(GraderStatus status) => status switch
    {
        GraderStatus.Reussi => "Réussi",
        GraderStatus.ARevoir => "À revoir",
        _ => "Non corrigé"
    };
}
