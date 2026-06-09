using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxFailClosedTests
{
    private sealed class ThrowingGrader : IGrader
    {
        public string Type => "io";
        public GraderResult Grade(GradingContext context, GradingStep step) =>
            throw new SandboxUnavailableException("binaire absent");
    }

    [Fact]
    public void Grade_SandboxUnavailable_FailsClosed_WithInternalError()
    {
        var grader = new ExerciseGrader(new IGrader[] { new ThrowingGrader() });
        var manifest = new ExerciseManifest
        {
            Id = "ex",
            Grading = { new GradingStep { Type = "io" } },
        };

        var result = grader.Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        var io = Assert.Single(result.Results);
        Assert.Equal(GraderStatus.ARevoir, io.Status);
        Assert.Contains(io.Messages, m => m.Contains("interne", System.StringComparison.OrdinalIgnoreCase));
    }
}
