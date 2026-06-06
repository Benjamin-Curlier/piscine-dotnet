using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GradersTests
{
    [Fact]
    public void Default_DispatchesMutationStep()
    {
        const string reference = """
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
        const string strongTests = """
            using Xunit;
            public class CompteTests
            {
                [Fact] public void A() { Assert.True(new Compte().Retirer(40)); }
                [Fact] public void B() { Assert.True(new Compte().Retirer(100)); }
            }
            """;
        var manifest = new Piscine.Core.Model.ExerciseManifest
        {
            Id = "ex03-mutation",
            Deliverables = { "CompteTests.cs" },
            Grading =
            {
                new Piscine.Core.Model.GradingStep
                {
                    Type = "mutation",
                    Reference = "reference/Compte.cs",
                    Mutants =
                    {
                        new Piscine.Core.Model.Mutant
                        {
                            Id = "borne", Label = "borne egale",
                            Find = "montant <= Solde", Replace = "montant < Solde",
                        },
                    },
                },
            },
        };
        var context = new GradingContext(
            new System.Collections.Generic.Dictionary<string, string> { ["CompteTests.cs"] = strongTests },
            new System.Collections.Generic.Dictionary<string, string> { ["reference/Compte.cs"] = reference });

        var result = Graders.Default().Grade(manifest, context);

        Assert.Contains(result.Results, r => r.GraderType == "mutation");
        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

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
