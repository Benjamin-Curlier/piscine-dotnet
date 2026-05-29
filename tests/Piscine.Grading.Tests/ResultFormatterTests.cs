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
    public void Format_Reussi_ShowsSuccess()
    {
        var result = new ExerciseGradingResult("ex00", new[] { GraderResult.Success("io") });

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Réussi", text);
    }

    [Fact]
    public void Format_NonCorrige_ShowsNotGraded()
    {
        var result = ExerciseGradingResult.NotGraded("ex02");

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Non corrigé", text);
    }
}
