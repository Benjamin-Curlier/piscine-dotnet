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
    public void Grade_ARevoir_WhenForbiddenDependencyViolatedViaFullyQualifiedName()
    {
        // Référence pleinement qualifiée, sans `using` : doit quand même être détectée.
        const string domainFq = """
            namespace Domain;
            public sealed class Compte
            {
                private readonly Infrastructure.SqlRepo _repo = new();
                public string Source => _repo.Nom;
            }
            """;
        var context = Sources(
            ("Domain/Compte.cs", domainFq),
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
    }

    [Fact]
    public void Grade_Reussi_WhenNamespacePrefixDoesNotCollide()
    {
        // From=Domain ne doit PAS matcher DomainServices ; To=Infrastructure ne doit PAS matcher InfrastructureHelpers.
        const string domainServices = """
            using InfrastructureHelpers;
            namespace DomainServices;
            public sealed class Aide { private readonly Helper _h = new(); }
            """;
        const string helpers = """
            namespace InfrastructureHelpers;
            public sealed class Helper { }
            """;
        var context = Sources(
            ("DomainServices/Aide.cs", domainServices),
            ("InfrastructureHelpers/Helper.cs", helpers));
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions
            {
                ForbiddenDependencies = { new LayerRule { From = "Domain", To = "Infrastructure" } },
            },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenForbiddenDependencyViolatedViaExtensionMethod()
    {
        // La dépendance ne NOMME jamais le type : elle transite par une méthode d'extension
        // (montant.Doubler()). L'ancienne détection (INamedTypeSymbol uniquement) la laissait échapper ;
        // elle doit désormais être vue via le type déclarant la méthode résolue.
        const string infraExtensions = """
            namespace Infrastructure;
            public static class MontantExtensions
            {
                public static int Doubler(this int valeur) => valeur * 2;
            }
            """;
        const string domainViaExtension = """
            using Infrastructure;
            namespace Domain;
            public sealed class Compte
            {
                public int Calcul(int montant) => montant.Doubler();
            }
            """;
        var context = Sources(
            ("Domain/Compte.cs", domainViaExtension),
            ("Infrastructure/MontantExtensions.cs", infraExtensions));
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
    public void Grade_Reussi_WhenExtensionMethodDependencyIsAllowedDirection()
    {
        // Application -> Domain via méthode d'extension : direction autorisée. Le nouveau chemin de
        // résolution des membres ne doit PAS introduire de faux positif ici (seule Domain -> Infrastructure
        // est interdite).
        const string domainExtensions = """
            namespace Domain;
            public static class MontantExtensions
            {
                public static int Doubler(this int valeur) => valeur * 2;
            }
            """;
        const string applicationViaExtension = """
            using Domain;
            namespace Application;
            public sealed class Service
            {
                public int Calcul(int montant) => montant.Doubler();
            }
            """;
        var context = Sources(
            ("Domain/MontantExtensions.cs", domainExtensions),
            ("Application/Service.cs", applicationViaExtension));
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions
            {
                ForbiddenDependencies = { new LayerRule { From = "Domain", To = "Infrastructure" } },
            },
        };

        var result = new ProjectGrader().Grade(context, step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ContentError_WhenNoCasesAndNoAssertions()
    {
        var step = new GradingStep { Type = "projet" };

        var result = new ProjectGrader().Grade(CleanProject(), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Grade_ContentError_WhenLayerRuleIncomplete()
    {
        var step = new GradingStep
        {
            Type = "projet",
            Project = new ProjectAssertions { ForbiddenDependencies = { new LayerRule { From = "Domain", To = "" } } },
        };

        var result = new ProjectGrader().Grade(CleanProject(), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
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
