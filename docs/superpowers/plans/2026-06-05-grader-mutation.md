# Grader Mutation (« élève-écrit-tests ») Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ajouter un type de grading `mutation` où la recrue livre des tests xUnit que le moteur confronte à une impl de référence cachée + des mutants (find/replace), avec verdict binaire « tous les mutants tués ».

**Architecture :** Nouveau `MutationGrader : IGrader` dans `Piscine.Grading`, réutilisant `CompilationService` et un runner xUnit extrait de `UnitGrader` (`XunitRunner`). Le modèle (`GradingStep`) gagne `Reference` + `Mutants` ; `SubmissionLoader` charge la référence cachée ; la gate `ContentValidator` valide gratuitement (le corrigé = la suite de tests modèle). Pilote : M13 `ex03-mutation`.

**Tech Stack :** C# / .NET 10, Roslyn (`Microsoft.CodeAnalysis`), xUnit (exécution par réflexion dans un `AssemblyLoadContext` collectible), YamlDotNet (underscore → PascalCase), `Piscine.slnx`.

**Référence design :** `docs/superpowers/specs/2026-06-05-grader-mutation-design.md`.

**Rappels environnement (HANDOFF « Pièges connus ») :**
- Lancer les tests via `dotnet test Piscine.slnx -c Release` (la solution est `.slnx`).
- `Piscine.Grading.Tests` désactive la parallélisation xUnit (Console global / ALC) — ne pas réactiver.
- En dev CLI, définir `$env:PISCINE_CONTENT="$PWD/content"`.
- `git commit` et `git push` = **appels séparés** ; ne pas pousser sans accord du proprio.
- WarningsAsErrors actif : tout `using` non utilisé ou warning casse le build.

---

## File Structure

**Créés :**
- `src/Piscine.Grading/XunitRunner.cs` — découverte + exécution des `[Fact]` dans un ALC collectible (extrait de `UnitGrader`). Responsabilité unique : exécuter un assembly de tests et rapporter `(FactCount, Failures, TimedOut)`.
- `src/Piscine.Grading/MutationGrader.cs` — le grader `mutation` + le helper pur `ApplyPatch`.
- `content/modules/13-tests-unitaires/exercises/ex03-mutation/**` — exercice pilote.

**Modifiés :**
- `src/Piscine.Core/Model/GradingStep.cs` — ajout `Reference` + `Mutants` + type `Mutant`.
- `src/Piscine.Core/Model/FeedbackTriggers.cs` — 2 nouveaux triggers.
- `src/Piscine.Grading/UnitGrader.cs` — refactor pour consommer `XunitRunner` (comportement inchangé).
- `src/Piscine.Grading/SubmissionLoader.cs` — charge `step.Reference` dans `GraderFiles`.
- `src/Piscine.Grading/Graders.cs` — enregistre `MutationGrader`.
- `content/modules/13-tests-unitaires/module.yaml` — ajoute `ex03-mutation` au groupe.
- `content/modules/13-tests-unitaires/cours.md` — section « tests qui attrapent les bugs ».
- `docs/wiki/Curriculum.md` — mention du nouvel exo.

---

## Task 1: Modèle — `Reference`, `Mutants`, triggers

**Files:**
- Modify: `src/Piscine.Core/Model/GradingStep.cs`
- Modify: `src/Piscine.Core/Model/FeedbackTriggers.cs`
- Test: `tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs`

- [ ] **Step 1: Écrire le test de parsing qui échoue**

Ajouter ce test dans `tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs` (classe existante ; ajuster le `namespace`/usings si besoin en s'alignant sur les autres tests du fichier) :

```csharp
[Fact]
public void Parses_MutationStep_WithReferenceAndMutants()
{
    const string yaml = """
        id: ex03-mutation
        title: "Mutation"
        objective: "Ecrire des tests qui attrapent les bugs."
        deliverables: [CompteTests.cs]
        grading:
          - type: mutation
            reference: reference/Compte.cs
            mutants:
              - id: borne-egal
                label: "Le retrait egal au solde n'est pas couvert."
                find: "amount > balance"
                replace: "amount >= balance"
        solution: [CompteTests.cs]
        """;

    var manifest = Piscine.Core.Io.YamlLoader.Deserialize<Piscine.Core.Model.ExerciseManifest>(yaml);
    var step = Assert.Single(manifest.Grading);

    Assert.Equal("mutation", step.Type);
    Assert.Equal("reference/Compte.cs", step.Reference);
    var mutant = Assert.Single(step.Mutants);
    Assert.Equal("borne-egal", mutant.Id);
    Assert.Equal("Le retrait egal au solde n'est pas couvert.", mutant.Label);
    Assert.Equal("amount > balance", mutant.Find);
    Assert.Equal("amount >= balance", mutant.Replace);
}
```

- [ ] **Step 2: Lancer le test, vérifier qu'il échoue**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Parses_MutationStep_WithReferenceAndMutants"`
Expected: échec de compilation (`GradingStep` n'a ni `Reference` ni `Mutants`, type `Mutant` inexistant).

- [ ] **Step 3: Ajouter `Reference` + `Mutants` + `Mutant` au modèle**

Dans `src/Piscine.Core/Model/GradingStep.cs`, ajouter dans la classe `GradingStep` (après `Blocking`) :

```csharp
    /// <summary>Pour le grader <c>mutation</c> : impl de référence cachée (chemin relatif au dossier content de l'exo).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Pour le grader <c>mutation</c> : mutations à dériver de la référence par find/replace.</summary>
    public List<Mutant> Mutants { get; set; } = new();
```

Et ajouter, en bas du même fichier (après la classe `IoCase`) :

```csharp
/// <summary>Une mutation : un remplacement textuel nommé appliqué à l'impl de référence (grader <c>mutation</c>).</summary>
public sealed class Mutant
{
    /// <summary>Identifiant court de la mutation (diagnostics auteur).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Phrase pédagogique montrée à la recrue si le mutant survit (le cas manquant).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Chaîne à trouver dans la référence (doit matcher exactement une fois).</summary>
    public string Find { get; set; } = string.Empty;

    /// <summary>Chaîne de remplacement (introduit le bug).</summary>
    public string Replace { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Ajouter les deux triggers de feedback**

Dans `src/Piscine.Core/Model/FeedbackTriggers.cs`, ajouter (après `UnitFailure`, avant `NormeViolation`) :

```csharp
    /// <summary>Les tests de la recrue échouent sur l'implémentation correcte (grader <c>mutation</c>).</summary>
    public const string TestsFailOnReference = "tests_fail_on_reference";

    /// <summary>Un mutant a survécu : un comportement bogué n'est pas détecté (grader <c>mutation</c>).</summary>
    public const string MutantSurvived = "mutant_survived";
```

Puis ajouter ces deux constantes au `HashSet` `All` :

```csharp
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        CompileError, IoMismatch, ExitCode, Timeout, RuntimeError, UnitFailure, NormeViolation,
        TestsFailOnReference, MutantSurvived,
    };
```

- [ ] **Step 5: Lancer le test, vérifier qu'il passe**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Parses_MutationStep_WithReferenceAndMutants"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Piscine.Core/Model/GradingStep.cs src/Piscine.Core/Model/FeedbackTriggers.cs tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs
git commit -m "feat(core): modele mutation (Reference, Mutants) + triggers"
```

---

## Task 2: Extraire `XunitRunner` de `UnitGrader`

But : sortir la découverte/exécution des `[Fact]` dans un helper réutilisable, **sans changer le comportement** de `UnitGrader` (couvert par `UnitGraderTests`).

**Files:**
- Create: `src/Piscine.Grading/XunitRunner.cs`
- Modify: `src/Piscine.Grading/UnitGrader.cs`
- Test (existant, régression) : `tests/Piscine.Grading.Tests/UnitGraderTests.cs`

- [ ] **Step 1: Créer `XunitRunner.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Piscine.Grading;

/// <summary>
/// Exécute les méthodes <c>[Fact]</c> d'un assembly compilé, dans un <see cref="AssemblyLoadContext"/>
/// collectible. Partagé par les graders <c>unit</c> et <c>mutation</c>.
/// </summary>
internal static class XunitRunner
{
    /// <summary>Chemins des assemblies xUnit à passer en références de compilation.</summary>
    public static readonly string[] References =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location
    };

    /// <summary>Résultat d'une exécution : nombre de tests trouvés, échecs, et drapeau de timeout.</summary>
    public sealed record RunResult(int FactCount, IReadOnlyList<string> Failures, bool TimedOut);

    public static RunResult Run(byte[] assemblyBytes, TimeSpan timeout)
    {
        var alc = new AssemblyLoadContext("xunit-run", isCollectible: true);
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var methods = FindFactMethods(assembly);
            if (methods.Count == 0)
            {
                return new RunResult(0, Array.Empty<string>(), false);
            }

            var failures = new List<string>();
            var task = Task.Run(() =>
            {
                foreach (var method in methods)
                {
                    var error = RunOne(method);
                    if (error is not null)
                    {
                        failures.Add($"{method.DeclaringType?.Name}.{method.Name} : {error}");
                    }
                }
            });

            return task.Wait(timeout)
                ? new RunResult(methods.Count, failures, false)
                : new RunResult(methods.Count, Array.Empty<string>(), true);
        }
        finally
        {
            alc.Unload();
        }
    }

    private static List<MethodInfo> FindFactMethods(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetParameters().Length == 0
                && m.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "Xunit.FactAttribute"))
            .ToList();

    private static string? RunOne(MethodInfo method)
    {
        try
        {
            var instance = Activator.CreateInstance(method.DeclaringType!);
            var result = method.Invoke(instance, Array.Empty<object>());
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
            }

            return null;
        }
        catch (TargetInvocationException ex)
        {
            return (ex.InnerException ?? ex).Message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
```

- [ ] **Step 2: Réécrire `UnitGrader.cs` pour consommer `XunitRunner`**

Remplacer **tout** le contenu de `src/Piscine.Grading/UnitGrader.cs` par :

```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>unit</c> : compile les sources de la recrue avec des tests xUnit cachés,
/// puis exécute les méthodes <c>[Fact]</c> par réflexion (un échec d'assertion = test KO).
/// </summary>
public sealed class UnitGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public string Type => "unit";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var sources = new Dictionary<string, string>(context.Sources);
        foreach (var (name, content) in context.GraderFiles)
        {
            sources[name] = content;
        }

        var compilation = CompilationService.Compile(
            sources,
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References);

        if (!compilation.Success)
        {
            var messages = new List<string> { "Le code ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        var run = XunitRunner.Run(compilation.AssemblyBytes, Timeout);
        if (run.TimedOut)
        {
            return GraderResult.Failure(Type, "Les tests ne se sont pas terminés à temps (boucle infinie ?).")
                .WithTrigger(FeedbackTriggers.Timeout);
        }

        if (run.FactCount == 0)
        {
            return GraderResult.Failure(Type, "Aucun test n'a été trouvé.").WithTrigger(FeedbackTriggers.UnitFailure);
        }

        return run.Failures.Count == 0
            ? GraderResult.Success(Type)
            : GraderResult.Failure(Type, run.Failures.ToArray()).WithTrigger(FeedbackTriggers.UnitFailure);
    }
}
```

- [ ] **Step 3: Lancer les tests `UnitGrader` (régression)**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~UnitGraderTests"`
Expected: PASS (3 tests verts, comportement identique).

- [ ] **Step 4: Commit**

```bash
git add src/Piscine.Grading/XunitRunner.cs src/Piscine.Grading/UnitGrader.cs
git commit -m "refactor(grading): extraire XunitRunner partage (unit + mutation)"
```

---

## Task 3: `ApplyPatch` (helper pur)

Le cœur testable isolément avant le grader complet.

**Files:**
- Create: `src/Piscine.Grading/MutationGrader.cs` (squelette + `ApplyPatch`)
- Test: `tests/Piscine.Grading.Tests/MutationGraderPatchTests.cs`

- [ ] **Step 1: Écrire les tests `ApplyPatch` qui échouent**

Créer `tests/Piscine.Grading.Tests/MutationGraderPatchTests.cs` :

```csharp
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class MutationGraderPatchTests
{
    [Fact]
    public void ApplyPatch_ReplacesSingleOccurrence()
    {
        var (result, count) = MutationGrader.ApplyPatch("if (a > b) return;", "> b", ">= b");

        Assert.Equal(1, count);
        Assert.Equal("if (a >= b) return;", result);
    }

    [Fact]
    public void ApplyPatch_ReturnsZero_WhenNotFound()
    {
        var (result, count) = MutationGrader.ApplyPatch("x = 1;", "absent", "autre");

        Assert.Equal(0, count);
        Assert.Equal("x = 1;", result);
    }

    [Fact]
    public void ApplyPatch_ReturnsTwo_WhenAmbiguous()
    {
        var (_, count) = MutationGrader.ApplyPatch("a + a", "a", "b");

        Assert.Equal(2, count);
    }

    [Fact]
    public void ApplyPatch_ReturnsZero_WhenFindEmpty()
    {
        var (result, count) = MutationGrader.ApplyPatch("abc", "", "x");

        Assert.Equal(0, count);
        Assert.Equal("abc", result);
    }
}
```

- [ ] **Step 2: Lancer, vérifier l'échec**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~MutationGraderPatchTests"`
Expected: échec de compilation (`MutationGrader` n'existe pas).

- [ ] **Step 3: Créer `MutationGrader.cs` avec `ApplyPatch` (le grader complet vient en Task 4)**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>mutation</c> : la recrue livre des tests xUnit ; le moteur les confronte à une
/// implémentation de référence cachée (doivent passer) puis à des mutants dérivés par find/replace
/// (chaque mutant doit être tué par ≥1 test rouge). Verdict binaire.
/// </summary>
public sealed class MutationGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public string Type => "mutation";

    public GraderResult Grade(GradingContext context, GradingStep step) =>
        throw new NotImplementedException("Implémenté en Task 4.");

    /// <summary>
    /// Applique un remplacement de chaîne ; renvoie la source modifiée et le nombre d'occurrences
    /// de <paramref name="find"/> trouvées (le remplacement n'est effectué que si ce nombre vaut 1).
    /// </summary>
    internal static (string Result, int Count) ApplyPatch(string source, string find, string replace)
    {
        if (string.IsNullOrEmpty(find))
        {
            return (source, 0);
        }

        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(find, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += find.Length;
        }

        if (count != 1)
        {
            return (source, count);
        }

        var position = source.IndexOf(find, StringComparison.Ordinal);
        var result = source[..position] + replace + source[(position + find.Length)..];
        return (result, 1);
    }
}
```

- [ ] **Step 4: Lancer, vérifier que ça passe**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~MutationGraderPatchTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Piscine.Grading/MutationGrader.cs tests/Piscine.Grading.Tests/MutationGraderPatchTests.cs
git commit -m "feat(grading): MutationGrader.ApplyPatch (find/replace mono-occurrence)"
```

---

## Task 4: `MutationGrader.Grade`

**Files:**
- Modify: `src/Piscine.Grading/MutationGrader.cs`
- Test: `tests/Piscine.Grading.Tests/MutationGraderTests.cs`

- [ ] **Step 1: Écrire les tests `Grade` qui échouent**

Créer `tests/Piscine.Grading.Tests/MutationGraderTests.cs`. Le contrat testé : une classe `Compte` avec `Retirer(int montant)` qui réussit (renvoie `true`) si `montant <= Solde`. Mutant : `<=` → `<` (laisse passer le retrait égal au solde).

```csharp
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
        // Test faux : attend l'échec d'un retrait pourtant valide sur la référence.
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
            new Dictionary<string, string>()); // pas de référence
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
}
```

- [ ] **Step 2: Lancer, vérifier l'échec**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~MutationGraderTests"`
Expected: échec — `Grade` jette `NotImplementedException`.

- [ ] **Step 3: Implémenter `Grade`**

Dans `src/Piscine.Grading/MutationGrader.cs`, remplacer la ligne `public GraderResult Grade(...) => throw new NotImplementedException(...);` par :

```csharp
    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        if (string.IsNullOrEmpty(step.Reference)
            || !context.GraderFiles.TryGetValue(step.Reference, out var reference))
        {
            return GraderResult.Failure(Type, $"contenu : implémentation de référence introuvable ({step.Reference}).");
        }

        // Passe 1 : les tests doivent compiler et passer sur l'implémentation correcte.
        var refCompile = CompileWith(context.Sources, step.Reference, reference);
        if (!refCompile.Success)
        {
            var messages = new List<string> { "Tes tests ne compilent pas contre l'API :" };
            messages.AddRange(refCompile.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        var refRun = XunitRunner.Run(refCompile.AssemblyBytes, Timeout);
        if (refRun.TimedOut)
        {
            return GraderResult.Failure(Type, "Tes tests ne se terminent pas à temps (boucle infinie ?).")
                .WithTrigger(FeedbackTriggers.Timeout);
        }

        if (refRun.FactCount == 0)
        {
            return GraderResult.Failure(Type, "Aucun test n'a été trouvé. Écris au moins un test.")
                .WithTrigger(FeedbackTriggers.MutantSurvived);
        }

        if (refRun.Failures.Count > 0)
        {
            var messages = new List<string> { "Tes tests échouent sur l'implémentation correcte :" };
            messages.AddRange(refRun.Failures);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.TestsFailOnReference);
        }

        // Passe 2..N : chaque mutant doit être tué (≥1 test rouge).
        var survivors = new List<string>();
        foreach (var mutant in step.Mutants)
        {
            var (mutated, count) = ApplyPatch(reference, mutant.Find, mutant.Replace);
            if (count != 1)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » : « {mutant.Find} » devrait matcher exactement une fois la référence (trouvé {count}).");
            }

            if (mutated == reference)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » : find et replace identiques, la référence est inchangée.");
            }

            var mutCompile = CompileWith(context.Sources, step.Reference, mutated);
            if (!mutCompile.Success)
            {
                return GraderResult.Failure(Type,
                    $"contenu : mutant « {mutant.Id} » ne compile pas : {string.Join(" ; ", mutCompile.Errors)}");
            }

            var mutRun = XunitRunner.Run(mutCompile.AssemblyBytes, Timeout);
            // Le mutant survit s'il termine sans aucun test rouge (un timeout = comportement attrapé = tué).
            if (!mutRun.TimedOut && mutRun.Failures.Count == 0)
            {
                survivors.Add(mutant.Label);
            }
        }

        if (survivors.Count > 0)
        {
            var messages = new List<string> { "Des comportements bogués ne sont pas détectés par tes tests :" };
            messages.AddRange(survivors.Select(label => $"- {label}"));
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.MutantSurvived);
        }

        return GraderResult.Success(Type);
    }

    private static CompilationResult CompileWith(
        IReadOnlyDictionary<string, string> studentSources, string referenceName, string referenceSource)
    {
        var sources = new Dictionary<string, string>(studentSources) { [referenceName] = referenceSource };
        return CompilationService.Compile(
            sources, OutputKind.DynamicallyLinkedLibrary, additionalReferences: XunitRunner.References);
    }
```

- [ ] **Step 4: Lancer, vérifier que ça passe**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~MutationGraderTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Piscine.Grading/MutationGrader.cs tests/Piscine.Grading.Tests/MutationGraderTests.cs
git commit -m "feat(grading): MutationGrader.Grade (reference + mutants, verdict binaire)"
```

---

## Task 5: `SubmissionLoader` charge la référence

**Files:**
- Modify: `src/Piscine.Grading/SubmissionLoader.cs`
- Test: `tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs`

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter dans `tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs` (suivre le style des tests existants du fichier — ils utilisent `TempDir` et écrivent un manifest + fichiers sur disque). Test :

```csharp
[Fact]
public void Load_PutsReferenceIntoGraderFiles()
{
    using var temp = new TempDir();
    var contentDir = Path.Combine(temp.Path, "content");
    var workspaceDir = Path.Combine(temp.Path, "workspace");
    Directory.CreateDirectory(Path.Combine(contentDir, "reference"));
    Directory.CreateDirectory(workspaceDir);

    File.WriteAllText(Path.Combine(contentDir, "manifest.yaml"), """
        id: ex03-mutation
        title: "Mutation"
        objective: "x"
        deliverables: [CompteTests.cs]
        grading:
          - type: mutation
            reference: reference/Compte.cs
            mutants:
              - id: m
                label: "l"
                find: "a"
                replace: "b"
        solution: [CompteTests.cs]
        """);
    File.WriteAllText(Path.Combine(contentDir, "reference", "Compte.cs"), "public class Compte { }");
    File.WriteAllText(Path.Combine(workspaceDir, "CompteTests.cs"), "// tests");

    var submission = SubmissionLoader.Load(contentDir, workspaceDir);

    Assert.True(submission.Context.GraderFiles.ContainsKey("reference/Compte.cs"));
    Assert.Contains("public class Compte", submission.Context.GraderFiles["reference/Compte.cs"]);
}
```

> Note : si `SubmissionLoaderTests` n'a pas déjà les usings `System.IO` / `Xunit`, les ajouter en tête de fichier.

- [ ] **Step 2: Lancer, vérifier l'échec**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Load_PutsReferenceIntoGraderFiles"`
Expected: FAIL (`GraderFiles` ne contient pas la référence).

- [ ] **Step 3: Charger la référence dans `SubmissionLoader`**

Dans `src/Piscine.Grading/SubmissionLoader.cs`, dans la boucle `foreach (var step in manifest.Grading)`, après la boucle interne `foreach (var testFile in step.TestFiles)`, ajouter :

```csharp
            if (!string.IsNullOrEmpty(step.Reference))
            {
                var referencePath = Path.Combine(exerciseContentDir, step.Reference);
                if (File.Exists(referencePath))
                {
                    graderFiles[step.Reference] = File.ReadAllText(referencePath);
                }
            }
```

- [ ] **Step 4: Lancer, vérifier que ça passe**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Load_PutsReferenceIntoGraderFiles"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Piscine.Grading/SubmissionLoader.cs tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs
git commit -m "feat(grading): SubmissionLoader charge l impl de reference (grader mutation)"
```

---

## Task 6: Enregistrer `MutationGrader`

**Files:**
- Modify: `src/Piscine.Grading/Graders.cs`
- Test: `tests/Piscine.Grading.Tests/GradersTests.cs`

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter dans `tests/Piscine.Grading.Tests/GradersTests.cs` un test qui vérifie qu'un exercice `mutation` est bien dispatché. S'aligner sur les assertions existantes du fichier ; si les tests existants vérifient la présence des types de graders, ajouter :

```csharp
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

    Assert.Equal(GraderStatus.Reussi, result.Status);
}
```

> `ExerciseGradingResult.Status` agrège les `GraderResult` ; vérifier le nom de la propriété de statut agrégé dans `ExerciseGradingResult.cs` et ajuster l'assertion si nécessaire (probablement `result.Status`).

- [ ] **Step 2: Lancer, vérifier l'échec**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Default_DispatchesMutationStep"`
Expected: FAIL — le step `mutation` n'est pas dispatché (aucun grader enregistré), donc résultat vide/non `Reussi`.

- [ ] **Step 3: Enregistrer le grader**

Remplacer le contenu de `src/Piscine.Grading/Graders.cs` par :

```csharp
namespace Piscine.Grading;

/// <summary>Fabrique l'ensemble standard de graders de la piscine.</summary>
public static class Graders
{
    public static ExerciseGrader Default() =>
        new(new IGrader[] { new IoGrader(), new NormeGrader(), new UnitGrader(), new MutationGrader() });
}
```

- [ ] **Step 4: Lancer, vérifier que ça passe**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~Default_DispatchesMutationStep"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Piscine.Grading/Graders.cs tests/Piscine.Grading.Tests/GradersTests.cs
git commit -m "feat(grading): enregistrer MutationGrader dans Graders.Default"
```

---

## Task 7: Validation de contenu (gate) — test d'intégration

Prouver que `ContentValidator` valide un exo `mutation` bien formé et rejette un mutant non tué, **sans nouveau code de gate** (la gate exécute déjà le corrigé via ses graders).

**Files:**
- Test: `tests/Piscine.Grading.Tests/ContentValidatorTests.cs`

- [ ] **Step 1: Écrire les tests d'intégration**

Étudier d'abord `tests/Piscine.Grading.Tests/ContentValidatorTests.cs` pour réutiliser son helper de construction d'un layout temporaire (les tests existants écrivent un module + exercice sur disque puis appellent `Validate`). En suivant **exactement** ce pattern, ajouter deux tests qui créent un module avec un exo `mutation` complet :

Structure à écrire sur disque (réutiliser le helper du fichier ; chemins indicatifs) :
- `module.yaml` : un groupe référençant `ex03-mutation`.
- `cours.md` à la racine du module (avec une ancre si `course_ref` est utilisée — sinon omettre `course_ref`).
- `exercises/ex03-mutation/manifest.yaml` (cf. ci-dessous).
- `exercises/ex03-mutation/subject.md` (non vide).
- `exercises/ex03-mutation/starter/Compte.cs` + `starter/CompteTests.cs`.
- `exercises/ex03-mutation/reference/Compte.cs` (impl correcte).
- `exercises/ex03-mutation/solution/CompteTests.cs` (suite modèle qui tue le mutant).

`manifest.yaml` :

```yaml
id: ex03-mutation
title: "Tests qui attrapent les bugs"
objective: "Ecrire des tests qui tuent les mutants."
deliverables: [CompteTests.cs]
starter: [Compte.cs, CompteTests.cs]
grading:
  - type: mutation
    reference: reference/Compte.cs
    mutants:
      - id: borne-egal
        label: "Le retrait d'un montant egal au solde n'est pas couvert."
        find: "montant <= Solde"
        replace: "montant < Solde"
feedback:
  hints:
    - when: mutant_survived
      message: "Pense au cas limite : retirer exactement le solde."
solution: [CompteTests.cs]
```

`reference/Compte.cs` :

```csharp
public class Compte
{
    public int Solde { get; private set; } = 100;
    public bool Retirer(int montant)
    {
        if (montant <= Solde) { Solde -= montant; return true; }
        return false;
    }
}
```

`solution/CompteTests.cs` (tue le mutant — teste la borne égale) :

```csharp
using Xunit;

public class CompteTests
{
    [Fact] public void Retirer_Inferieur_Reussit() => Assert.True(new Compte().Retirer(40));
    [Fact] public void Retirer_EgalAuSolde_Reussit() => Assert.True(new Compte().Retirer(100));
    [Fact] public void Retirer_Superieur_Echoue() => Assert.False(new Compte().Retirer(101));
}
```

`starter/Compte.cs` (stub) :

```csharp
public class Compte
{
    public int Solde { get; private set; } = 100;
    public bool Retirer(int montant) => throw new System.NotImplementedException();
}
```

`starter/CompteTests.cs` :

```csharp
using Xunit;

public class CompteTests
{
    [Fact] public void Exemple() => Assert.True(new Compte().Retirer(40));
}
```

Tests :

```csharp
[Fact]
public void Validate_AcceptsWellFormedMutationExercise()
{
    var layout = /* construire le layout via le helper du fichier */;
    var report = new ContentValidator(Graders.Default()).Validate(layout);
    Assert.True(report.IsValid, string.Join(" | ", report.Issues.Select(i => i.Message)));
}

[Fact]
public void Validate_RejectsMutationExercise_WhenSolutionDoesNotKillMutant()
{
    // Même layout, mais la solution n'a PAS le test de la borne égale -> mutant survit.
    var layout = /* layout avec solution/CompteTests.cs réduit à Retirer_Inferieur_Reussit */;
    var report = new ContentValidator(Graders.Default()).Validate(layout);
    Assert.False(report.IsValid);
    Assert.Contains(report.Issues, i => i.Message.Contains("graders"));
}
```

> Adapter la construction du `PiscineLayout` au helper réellement présent dans `ContentValidatorTests`. Si aucun helper réutilisable n'existe, créer une méthode privée `BuildMutationLayout(TempDir, bool strongSolution)` dans la classe de test qui écrit l'arborescence ci-dessus.

- [ ] **Step 2: Lancer les tests**

Run: `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName~ContentValidatorTests"`
Expected: les deux nouveaux tests PASS (acceptation + rejet), aucun test existant cassé.

- [ ] **Step 3: Commit**

```bash
git add tests/Piscine.Grading.Tests/ContentValidatorTests.cs
git commit -m "test(grading): gate valide/rejette un exo mutation (corrige = suite modele)"
```

---

## Task 8: Contenu pilote — M13 `ex03-mutation`

**Files:**
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/manifest.yaml`
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/subject.md`
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/starter/Compte.cs`
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/starter/CompteTests.cs`
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/reference/Compte.cs`
- Create: `content/modules/13-tests-unitaires/exercises/ex03-mutation/solution/CompteTests.cs`
- Modify: `content/modules/13-tests-unitaires/module.yaml`
- Modify: `content/modules/13-tests-unitaires/cours.md`
- Modify: `docs/wiki/Curriculum.md`

> Suivre la « MÉTHODE pour ajouter du contenu » du HANDOFF : écrire la **référence d'abord**, puis la **solution modèle**, puis les mutants, et itérer avec `validate-content` jusqu'à ce que tous les mutants soient tués. Respecter les **RÈGLES CRITIQUES du grader** (usings explicites ; pas d'implicit using). Ici la référence/solution sont des bibliothèques (pas de top-level statements) : pas de souci `Console`.

- [ ] **Step 1: Créer les fichiers de l'exercice**

Réutiliser les contenus de la Task 7 (référence, solution, starter, manifest) comme contenu pilote, en enrichissant éventuellement avec un 2e mutant pédagogique. `manifest.yaml` recommandé (avec `course_ref` vers une ancre créée au Step 4) :

```yaml
id: ex03-mutation
title: "Des tests qui attrapent les bugs"
objective: "Ecrire une suite de tests xUnit qui detecte les implementations boguees (mutants)."
difficulty: moyen
deliverables: [CompteTests.cs]
starter: [Compte.cs, CompteTests.cs]
grading:
  - type: mutation
    reference: reference/Compte.cs
    mutants:
      - id: borne-egal
        label: "Le retrait d'un montant egal au solde n'est pas couvert."
        find: "montant <= Solde"
        replace: "montant < Solde"
      - id: solde-non-debite
        label: "Apres un retrait, le solde doit diminuer du montant retire."
        find: "Solde -= montant"
        replace: "Solde -= 0"
feedback:
  hints:
    - when: mutant_survived
      message: "Un test qui ne casse jamais ne sert a rien. Couvre les cas limites et verifie l'effet de bord sur le solde."
    - when: tests_fail_on_reference
      message: "Tes tests doivent d'abord passer sur une implementation correcte. Relis le contrat dans le sujet."
  course_ref: "cours.md#mutation"
solution: [CompteTests.cs]
```

`reference/Compte.cs` (correct, tue-able par les 2 mutants) :

```csharp
public class Compte
{
    public int Solde { get; private set; } = 100;

    public bool Retirer(int montant)
    {
        if (montant <= Solde)
        {
            Solde -= montant;
            return true;
        }

        return false;
    }
}
```

`solution/CompteTests.cs` (modèle qui tue les 2 mutants — `borne-egal` via le retrait de 100, `solde-non-debite` via la vérification du solde après retrait) :

```csharp
using Xunit;

public class CompteTests
{
    [Fact]
    public void Retirer_MontantInferieur_Reussit_EtDebiteLeSolde()
    {
        var compte = new Compte();
        Assert.True(compte.Retirer(40));
        Assert.Equal(60, compte.Solde);
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
```

`starter/Compte.cs` :

```csharp
public class Compte
{
    public int Solde { get; private set; } = 100;

    // A toi de tester : Retirer renvoie true et debite le solde si montant <= Solde, sinon false.
    public bool Retirer(int montant) => throw new System.NotImplementedException();
}
```

`starter/CompteTests.cs` :

```csharp
using Xunit;

public class CompteTests
{
    [Fact]
    public void Exemple_ARemplacer()
    {
        // Remplace par de vrais tests du contrat de Compte.
        Assert.True(new Compte().Retirer(40));
    }
}
```

`subject.md` : décrire le contrat de `Compte` (propriété `Solde` initialisée à 100, méthode `Retirer(int montant)` → `true` + débite si `montant <= Solde`, sinon `false`), expliquer que l'impl est fournie cachée et que **le but est d'écrire des tests qui détectent des versions boguées**. Suivre le ton/format des `subject.md` voisins (`ex00`, `ex02`).

- [ ] **Step 2: Ajouter l'exo au `module.yaml`**

Modifier `content/modules/13-tests-unitaires/module.yaml` :

```yaml
    exercises: [ex00-assertion, ex01-aaa, ex02-cas-limites, ex03-mutation]
```

- [ ] **Step 3: Valider le contenu (boucle auteur)**

Run (PowerShell) :
```powershell
$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content
```
Expected: **« Contenu valide. »**

Si un mutant survit (la gate le signale), c'est que la `solution/CompteTests.cs` ne le tue pas → renforcer la suite modèle ou ajuster le mutant, puis relancer. Si un `find` ne matche pas exactement une fois, ajuster la chaîne `find` pour qu'elle soit unique dans `reference/Compte.cs`.

- [ ] **Step 4: Ajouter la section de cours + l'ancre `#mutation`**

Dans `content/modules/13-tests-unitaires/cours.md`, ajouter une section avec un titre produisant l'ancre `mutation` (vérifier la convention d'ancres du projet via `CourseAnchors`), p.ex. :

```markdown
## Mutation : écrire des tests qui attrapent les bugs

Un test qui ne casse jamais ne sert à rien. Pour savoir si tes tests sont utiles,
on « mute » le code correct (on y introduit un bug) : un bon test doit alors **échouer**.
Un mutant qui survit = un comportement que tes tests ne vérifient pas. Pense aux **cas
limites** (la borne `<=` vs `<`) et aux **effets de bord** (le solde a-t-il bien changé ?).
```

Re-valider :
```powershell
$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content
```
Expected: **« Contenu valide. »** (l'ancre `#mutation` est résolue).

- [ ] **Step 5: Mettre à jour le Curriculum**

Dans `docs/wiki/Curriculum.md`, repérer la ligne du module M13 et mentionner le nouvel exo `ex03-mutation` (type `mutation`, « écrire des tests qui tuent des mutants »). Suivre le format des entrées voisines.

- [ ] **Step 6: Suite complète + commit**

Run: `dotnet test Piscine.slnx -c Release`
Expected: tous verts (≥ 78 + les nouveaux).

```bash
git add content/modules/13-tests-unitaires docs/wiki/Curriculum.md
git commit -m "content(m13): ex03-mutation (grader eleve-ecrit-tests, mutants)"
```

---

## Task 9: MAJ HANDOFF + BLOCKERS

**Files:**
- Modify: `docs/superpowers/HANDOFF.md`
- Modify (branche `v1.0-blockers`) : `docs/superpowers/BLOCKERS-v1.0.md`

- [ ] **Step 1: Mettre à jour le HANDOFF**

Dans `docs/superpowers/HANDOFF.md` : noter que le **Point 6 (grader `mutation` / élève-écrit-tests) est FAIT** (nouveau type de grading branché, pilote M13 `ex03-mutation`), mettre à jour le compte de tests et la liste des graders (`io`/`unit`/`norme`/**`mutation`**), et ajuster l'ordre des prochaines étapes (reste : Point 4 grader git, Point 3 Rush 3, Point 5 réseau).

- [ ] **Step 2: Cocher le Point 6 dans BLOCKERS**

`BLOCKERS-v1.0.md` vit sur la branche `v1.0-blockers`. Soit annoter depuis `main` via une note « Point 6 fait sur main (commit …) », soit (si le proprio le souhaite) mettre à jour le doc sur sa branche. Demander la préférence du proprio avant de toucher à `v1.0-blockers`.

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/HANDOFF.md
git commit -m "docs(handoff): grader mutation (Point 6) fait + etat moteur"
```

- [ ] **Step 4: Pousser (avec accord)**

Demander le **go** au proprio, puis :
```bash
git push origin main
```
(commit et push = appels séparés). Vérifier la CI :
```bash
gh run list --branch main --limit 1 --json status,conclusion
```

---

## Notes de mise en œuvre

- **Déterminisme** : référence, mutants et tests recrue doivent être déterministes (règle grader). La somme `Solde`/`Retirer` l'est.
- **WarningsAsErrors** : retirer tout `using` inutile dans les nouveaux fichiers ; `XunitRunner` et `MutationGrader` listent leurs usings au plus juste.
- **Timeout-comme-tué** : un mutant qui fait boucler les tests est considéré **tué** (la passe référence a déjà prouvé que les tests terminent). Documenté dans le code.
- **Pas de support `try` dédié** : la boucle auteur passe par `validate-content` (qui exécute la `solution` via le grader et liste les survivants). Hors périmètre, cf. spec §11.
