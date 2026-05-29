using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GradersTests
{
    [Fact]
    public void Default_GradesIoNormeAndUnit()
    {
        var manifest = new ExerciseManifest
        {
            Id = "ex",
            Grading =
            {
                new GradingStep { Type = "io", Cases = { new IoCase { ExpectStdout = "x", ExpectExit = 0 } } },
                new GradingStep { Type = "norme", Blocking = false }
            }
        };
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["P.cs"] = "System.Console.Write(\"x\");"
        });

        var result = Graders.Default().Grade(manifest, context);

        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Equal(2, result.Results.Count);
    }
}
