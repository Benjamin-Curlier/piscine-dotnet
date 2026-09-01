using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public sealed class FileGraderTests
{
    [Fact]
    public void Grade_MissingAndForbiddenFragments_ReportsFileConstraint()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Dockerfile"] = "FROM image:latest\n"
        });
        var step = new GradingStep
        {
            Type = "fichier",
            File = new FileAssertions
            {
                Rules =
                {
                    new FileRule
                    {
                        Path = "Dockerfile",
                        RequiredFragments = { "USER $APP_UID" },
                        ForbiddenFragments = { ":latest" }
                    }
                }
            }
        };

        var result = new FileGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.FileConstraint, result.Trigger);
        Assert.Equal(2, result.Messages.Count);
    }

    [Fact]
    public void Grade_AllFileRulesSatisfied_Succeeds()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["AppHost.cs"] = "builder.AddContainer(\"nats\");"
        });
        var step = new GradingStep
        {
            Type = "fichier",
            File = new FileAssertions
            {
                Rules = { new FileRule { Path = "AppHost.cs", RequiredFragments = { "AddContainer" } } }
            }
        };

        Assert.Equal(GraderStatus.Reussi, new FileGrader().Grade(context, step).Status);
    }
}
