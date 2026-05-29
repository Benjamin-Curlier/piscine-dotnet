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

        if (result.Status == GraderStatus.ARevoir && !string.IsNullOrWhiteSpace(feedback.CourseRef))
        {
            sb.AppendLine($"→ Revois le cours : {feedback.CourseRef}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string Label(GraderStatus status) => status switch
    {
        GraderStatus.Reussi => "Réussi",
        GraderStatus.ARevoir => "À revoir",
        _ => "Non corrigé"
    };
}
