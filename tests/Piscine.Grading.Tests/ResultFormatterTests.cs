using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ResultFormatterTests
{
    [Fact]
    public void Format_ARevoir_ShowsMessagesAndCourseRef()
    {
        var result = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Failure("io", "La sortie ne correspond pas.")
        });
        var feedback = new FeedbackConfig { CourseRef = "cours.md#hello" };

        var text = ResultFormatter.Format(result, feedback);

        Assert.Contains("ex00", text);
        Assert.Contains("À revoir", text);
        Assert.Contains("La sortie ne correspond pas.", text);
        Assert.Contains("cours.md#hello", text);
    }

    [Fact]
    public void Format_ARevoir_ShowsHintMatchingTrigger()
    {
        var result = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Failure("io", "La sortie ne correspond pas.").WithTrigger(FeedbackTriggers.IoMismatch)
        });
        var feedback = new FeedbackConfig
        {
            Hints =
            {
                new FeedbackHint { When = FeedbackTriggers.CompileError, Message = "Vérifie la syntaxe." },
                new FeedbackHint { When = FeedbackTriggers.IoMismatch, Message = "Compte les espaces et les retours ligne." },
            },
        };

        var text = ResultFormatter.Format(result, feedback);

        Assert.Contains("Indice : Compte les espaces", text);
        Assert.DoesNotContain("Vérifie la syntaxe.", text);
    }

    [Fact]
    public void Format_ARevoir_NoHintWhenTriggerHasNoMatch()
    {
        var result = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Failure("io", "Code de sortie inattendu.").WithTrigger(FeedbackTriggers.ExitCode)
        });
        var feedback = new FeedbackConfig
        {
            Hints = { new FeedbackHint { When = FeedbackTriggers.IoMismatch, Message = "indice io" } },
        };

        var text = ResultFormatter.Format(result, feedback);

        Assert.DoesNotContain("Indice :", text);
    }

    [Fact]
    public void Format_Reussi_ShowsSuccess()
    {
        var result = new ExerciseGradingResult("ex00", new[] { GraderResult.Success("io") });

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Réussi", text);
    }

    [Fact]
    public void EmptySubmission_ShowsEducationalGuidance()
    {
        var text = ResultFormatter.EmptySubmission("ex00");

        Assert.Contains("Aucun fichier rendu", text);
        Assert.Contains("piscine start ex00", text);
        Assert.DoesNotContain("Main", text);
    }

    [Fact]
    public void Format_NonCorrige_ShowsNotGraded()
    {
        var result = ExerciseGradingResult.NotGraded("ex02");

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Non corrigé", text);
    }
}
