using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ProjectGraderTests
{
    // --- Fixtures multi-couches (file-scoped namespaces, usings explicites) ---

    private const string DomainPropre = """
        namespace Domain;
        public sealed class Compte
        {
            public int Solde { get; private set; }
            public void Crediter(int montant) => Solde += montant;
        }
        """;

    private const string DomainAvecInfra = """
        using Infrastructure;
        namespace Domain;
        public sealed class Compte
        {
            private readonly SqlRepo _repo = new();
            public string Source => _repo.Nom;
        }
        """;

    private const string Application = """
        using Domain;
        namespace Application;
        public sealed class Service
        {
            public int SoldeApres(int montant)
            {
                var c = new Compte();
                c.Crediter(montant);
                return c.Solde;
            }
        }
        """;

    private const string Infrastructure = """
        namespace Infrastructure;
        public sealed class SqlRepo
        {
            public string Nom => "sql";
        }
        """;

    private static GradingContext Sources(params (string Name, string Content)[] files)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (name, content) in files)
        {
            dict[name] = content;
        }

        return new GradingContext(dict);
    }

    private static GradingContext CleanProject() => Sources(
        ("Domain/Compte.cs", DomainPropre),
        ("Application/Service.cs", Application),
        ("Infrastructure/SqlRepo.cs", Infrastructure));

    [Fact]
    public void Grade_Reussi_WhenRequiredTypesPresentAndLayersRespected()
    {
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions
            {
                RequiresTypes = { "Domain.Compte", "Application.Service", "Infrastructure.SqlRepo" },
                ForbiddenDependencies = { new LayerRule { From = "Domain", To = "Infrastructure" } },
            },
        };

        var result = new ProjectGrader().Grade(CleanProject(), step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenRequiredTypeMissing()
    {
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions { RequiresTypes = { "Domain.Inexistant" } },
        };

        var result = new ProjectGrader().Grade(CleanProject(), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.ProjectStructure, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("Domain.Inexistant"));
    }

    [Fact]
    public void Grade_ARevoir_WhenForbiddenDependencyViolated()
    {
        var context = Sources(
            ("Domain/Compte.cs", DomainAvecInfra),
            ("Infrastructure/SqlRepo.cs", Infrastructure));
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions
            {
                ForbiddenDependencies = { new LayerRule { From = "Domain", To = "Infrastructure" } },
            },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.ProjectStructure, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("couche interdite"));
    }

    [Fact]
    public void Grade_Reussi_WhenLayerDependencyIsAllowedDirection()
    {
        // Application -> Domain est autorisé ; seule Domain -> Infrastructure est interdite.
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions
            {
                ForbiddenDependencies = { new LayerRule { From = "Domain", To = "Infrastructure" } },
            },
        };

        var result = new ProjectGrader().Grade(CleanProject(), step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Reussi_WhenIoCasesPassAcrossMultipleFiles()
    {
        var context = Sources(
            ("Program.cs", "var g = new Greeter();\nSystem.Console.WriteLine(g.Salut(\"monde\"));"),
            ("Greeter.cs", "public sealed class Greeter { public string Salut(string nom) => $\"Bonjour {nom}\"; }"));
        var step = new GradingStep
        {
            Type = "projet",
            Cases = { new IoCase { ExpectStdout = "Bonjour monde\n", ExpectExit = 0 } },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenIoOutputDiffers()
    {
        var context = Sources(
            ("Program.cs", "System.Console.WriteLine(\"faux\");"));
        var step = new GradingStep
        {
            Type = "projet",
            Cases = { new IoCase { ExpectStdout = "attendu\n", ExpectExit = 0 } },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.IoMismatch, result.Trigger);
    }

    [Fact]
    public void Grade_ARevoir_WhenProjectDoesNotCompile()
    {
        var context = Sources(("Cassé.cs", "namespace Domain; public class Compte { public void X( }"));
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions { RequiresTypes = { "Domain.Compte" } },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }
}
