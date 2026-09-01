using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public sealed class SourceGraderTests
{
    [Fact]
    public void Grade_ForbiddenFragment_ReportsFileAndTrigger()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Base2.cs"] = "System.Console.WriteLine(Convert.ToString(n, 2));"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions { ForbiddenFragments = { "Convert.ToString" } }
        };

        var result = new SourceGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.SourceConstraint, result.Trigger);
        Assert.Contains(result.Messages, message => message.Contains("Base2.cs"));
    }

    [Fact]
    public void Grade_RequiredAndForbiddenConstraintsSatisfied_Succeeds()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Base2.cs"] = "var bit = n % 2;"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions
            {
                RequiredFragments = { "% 2" },
                ForbiddenFragments = { "Convert.ToString" }
            }
        };

        Assert.Equal(GraderStatus.Reussi, new SourceGrader().Grade(context, step).Status);
    }

    [Fact]
    public void Grade_ForbiddenFragmentMentionedOnlyInComment_DoesNotFail()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Base2.cs"] = "// Ne pas utiliser Convert.ToString\nvar bit = n % 2;"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions { ForbiddenFragments = { "Convert.ToString" } }
        };

        Assert.Equal(GraderStatus.Reussi, new SourceGrader().Grade(context, step).Status);
    }

    [Fact]
    public void Grade_RequiredOccurrences_IgnoresCommentsAndReportsShortfall()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Pipeline.cs"] = "Handle(request); // Handle(request);\nHandle(request);"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions
            {
                RequiredOccurrences =
                {
                    new SourceOccurrence { Fragment = "Handle(request)", Count = 3 }
                }
            }
        };

        var result = new SourceGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.SourceConstraint, result.Trigger);
        Assert.Contains(result.Messages, message =>
            message.Contains("présent 2 fois") && message.Contains("3 occurrence"));
    }

    [Fact]
    public void Grade_SemanticInvocationAndLock_RejectsSuperficialKeywords()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Lock.cs"] = "var n = 3; lock (new object()) { } System.Console.WriteLine(n);"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions
            {
                RequireCompilation = true,
                RequiredSyntax = { "lock" },
                RequiredInvocations = { "System.Threading.Tasks.Parallel.For" }
            }
        };

        var result = new SourceGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, message => message.Contains("Parallel.For"));
    }

    [Fact]
    public void Grade_SemanticInvocationAndLock_AcceptsResolvedTechnique()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Lock.cs"] = """
                var gate = new object();
                var count = 0;
                System.Threading.Tasks.Parallel.For(0, 3, _ => { lock (gate) { count++; } });
                System.Console.WriteLine(count);
                """
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions
            {
                RequireCompilation = true,
                RequiredSyntax = { "lock" },
                RequiredInvocations = { "System.Threading.Tasks.Parallel.For" }
            }
        };

        Assert.Equal(GraderStatus.Reussi, new SourceGrader().Grade(context, step).Status);
    }

    [Fact]
    public void Grade_RequireCompilation_RejectsInvalidCSharp()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Broken.cs"] = "ceci n'est pas du C#"
        });
        var step = new GradingStep
        {
            Type = "source",
            Source = new SourceAssertions { RequireCompilation = true }
        };

        var result = new SourceGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, message => message.Contains("doit compiler"));
    }
}
