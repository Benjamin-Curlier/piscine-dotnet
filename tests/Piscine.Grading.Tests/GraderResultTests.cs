using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GraderResultTests
{
    [Fact]
    public void Success_HasReussiStatus()
    {
        var result = GraderResult.Success("io");

        Assert.Equal("io", result.GraderType);
        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Failure_HasARevoirStatusAndMessages()
    {
        var result = GraderResult.Failure("io", "Sortie inattendue.");

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(new[] { "Sortie inattendue." }, result.Messages);
    }

    [Fact]
    public void Aggregate_IsARevoir_WhenAnyResultIsARevoir()
    {
        var aggregate = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Success("norme"),
            GraderResult.Failure("io", "KO")
        });

        Assert.Equal("ex00", aggregate.ExerciseId);
        Assert.Equal(GraderStatus.ARevoir, aggregate.Status);
    }

    [Fact]
    public void Aggregate_IsReussi_WhenAllResultsReussi()
    {
        var aggregate = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Success("io"),
            GraderResult.Success("norme")
        });

        Assert.Equal(GraderStatus.Reussi, aggregate.Status);
    }
}
