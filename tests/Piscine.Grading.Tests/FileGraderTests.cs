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

    [Fact]
    public void Grade_XmlAssertions_ParseStructureValuesAndAttributes()
    {
        var context = new GradingContext(new Dictionary<string, string>
        {
            ["Versioning.props"] = """
                <Project>
                  <PropertyGroup>
                    <Deterministic>true</Deterministic>
                    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">
                      true
                    </ContinuousIntegrationBuild>
                  </PropertyGroup>
                </Project>
                """
        });
        var rule = new FileRule { Path = "Versioning.props" };
        rule.RequiredXmlElements.Add(new XmlElementAssertion
        {
            Path = "Project/PropertyGroup/Deterministic",
            Value = "true"
        });
        rule.RequiredXmlElements.Add(new XmlElementAssertion
        {
            Path = "Project/PropertyGroup/ContinuousIntegrationBuild",
            Value = "true",
            Attributes = { ["Condition"] = "'$(CI)' == 'true'" }
        });
        var step = new GradingStep
        {
            Type = "fichier",
            File = new FileAssertions { Rules = { rule } }
        };

        Assert.Equal(GraderStatus.Reussi, new FileGrader().Grade(context, step).Status);
    }

    [Theory]
    [InlineData("<Project><PropertyGroup>", 1)]
    [InlineData("<Project><PropertyGroup><Deterministic>false</Deterministic></PropertyGroup></Project>", 1)]
    public void Grade_InvalidOrWrongXml_Fails(string xml, int expectedMessageCount)
    {
        var context = new GradingContext(new Dictionary<string, string> { ["Versioning.props"] = xml });
        var rule = new FileRule { Path = "Versioning.props" };
        rule.RequiredXmlElements.Add(new XmlElementAssertion
        {
            Path = "Project/PropertyGroup/Deterministic",
            Value = "true"
        });
        var step = new GradingStep
        {
            Type = "fichier",
            File = new FileAssertions { Rules = { rule } }
        };

        var result = new FileGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(expectedMessageCount, result.Messages.Count);
    }
}
