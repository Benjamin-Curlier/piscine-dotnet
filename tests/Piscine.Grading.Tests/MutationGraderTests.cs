using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class MutationGraderTests
{
    private const string ReferenceName = "reference/Compte.cs";

    // Référence correcte : autorise le retrait si montant <= Solde.
    private const string Reference = """
        public class Compte
        {
            public int Solde { get; private set; } = 100;
            public bool Retirer(int montant)
            {
                if (montant <= Solde) { Solde -= montant; return true; }
                return false;
            }
        }
        """;

    private static GradingStep Step() => new()
    {
        Type = "mutation",
        Reference = ReferenceName,
        Mutants =
        {
            new Mutant
            {
                Id = "borne-egal",
                Label = "Le retrait d'un montant égal au solde n'est pas couvert.",
                Find = "montant <= Solde",
                Replace = "montant < Solde",
            },
        },
    };

    private static GradingContext Context(string learnerTests)
    {
        var sources = new Dictionary<string, string> { ["CompteTests.cs"] = learnerTests };
        var graderFiles = new Dictionary<string, string> { [ReferenceName] = Reference };
        return new GradingContext(sources, graderFiles);
    }

    // Suite complète : couvre le retrait égal au solde -> tue le mutant.
    private const string StrongTests = """
        using Xunit;

        public class CompteTests
        {
            [Fact]
            public void Retirer_MontantInferieur_Reussit()
            {
                Assert.True(new Compte().Retirer(40));
            }

            [Fact]
            public void Retirer_MontantEgalAuSolde_Reussit()
            {
                Assert.True(new Compte().Retirer(100));
            }

            [Fact]
            public void Retirer_MontantSuperieur_Echoue()
            {
                Assert.False(new Compte().Retirer(101));
            }
        }
        """;

    // Suite faible : ne teste jamais la borne égale -> le mutant survit.
    private const string WeakTests = """
        using Xunit;

        public class CompteTests
        {
            [Fact]
            public void Retirer_MontantInferieur_Reussit()
            {
                Assert.True(new Compte().Retirer(40));
            }
        }
        """;

    [Fact]
    public void Grade_Reussi_WhenAllMutantsKilled()
    {
        var result = new MutationGrader().Grade(Context(StrongTests), Step());

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenMutantSurvives_RevealsLabel()
    {
        var result = new MutationGrader().Grade(Context(WeakTests), Step());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.MutantSurvived, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("égal au solde"));
    }

    [Fact]
    public void Grade_ARevoir_WhenTestsFailOnReference()
    {
        const string wrongTests = """
            using Xunit;

            public class CompteTests
            {
                [Fact]
                public void Faux()
                {
                    Assert.False(new Compte().Retirer(10));
                }
            }
            """;

        var result = new MutationGrader().Grade(Context(wrongTests), Step());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.TestsFailOnReference, result.Trigger);
    }

    [Fact]
    public void Grade_ARevoir_WhenTestsDoNotCompile()
    {
        const string broken = "using Xunit; public class CompteTests { [Fact] public void X() { Assert.True( } }";

        var result = new MutationGrader().Grade(Context(broken), Step());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }

    [Fact]
    public void Grade_ContentError_WhenReferenceMissing()
    {
        var context = new GradingContext(
            new Dictionary<string, string> { ["CompteTests.cs"] = StrongTests },
            new Dictionary<string, string>());
        var result = new MutationGrader().Grade(context, Step());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Grade_ContentError_WhenPatchDoesNotApply()
    {
        var step = Step();
        step.Mutants[0].Find = "introuvable-dans-la-reference";
        var result = new MutationGrader().Grade(Context(StrongTests), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Grade_ARevoir_WhenNoTestsProvided()
    {
        const string noTests = """
            public class CompteTests
            {
                public void PasUnTest()
                {
                    _ = new Compte().Retirer(40);
                }
            }
            """;

        var result = new MutationGrader().Grade(Context(noTests), Step());

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.MutantSurvived, result.Trigger);
        Assert.Contains(result.Messages, m => m.Contains("Aucun test"));
    }
}
