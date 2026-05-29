# Itération 3 — Grader unit + orchestration séquentielle — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (ou subagent-driven-development) pour exécuter tâche par tâche. Cases `- [ ]` pour le suivi.

**Goal:** Ajouter le grader `unit` (exécution de tests xUnit cachés) et l'orchestration séquentielle par groupe (stop au 1er KO → *Non corrigé*), avec report dans la progression.

**Architecture:** Un `GradingContext` (sources recrue + fichiers grader cachés) remplace le simple dictionnaire passé aux graders. `UnitGrader` compile sources + tests cachés en DLL (références xUnit ajoutées), charge l'assembly dans un `AssemblyLoadContext` collectible, exécute par réflexion les méthodes `[Fact]` (un échec d'assertion = test KO) sous timeout, et agrège un feedback. `GroupGrader` corrige une liste ordonnée de soumissions et s'arrête au premier KO (les suivantes → `NonCorrige`). `ProgressRecorder` applique les résultats à un `Progress` persistable. L'assemblage depuis le disque (workspace + content) est repoussé à l'It.4 (CLI) — ici tout est testé sur des entrées en mémoire.

**Tech Stack:** .NET 10, Roslyn, xunit.assert + xunit.core (exécution réflexive), `AssemblyLoadContext`, xUnit v2 (tests).

**Contexte repo (It.0→It.2p1 faites) :** `Piscine.Grading` contient `CompilationService`, `IGrader`, `IoGrader`, `NormeGrader`, `ExerciseGrader`, modèle de résultat. `IGrader.Grade` prend aujourd'hui `(IReadOnlyDictionary<string,string> sources, GradingStep)`. Tests Grading : parallélisation désactivée. `Directory.Build.props` : Nullable + TreatWarningsAsErrors. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Grading/GradingContext.cs` | Entrées d'une correction : sources recrue + fichiers grader |
| `src/Piscine.Grading/IGrader.cs` | (modifié) `Grade(GradingContext, GradingStep)` |
| `src/Piscine.Grading/IoGrader.cs` | (modifié) lit `context.Sources` |
| `src/Piscine.Grading/NormeGrader.cs` | (modifié) lit `context.Sources` |
| `src/Piscine.Grading/ExerciseGrader.cs` | (modifié) `Grade(ExerciseManifest, GradingContext)` |
| `src/Piscine.Grading/CompilationService.cs` | (modifié) références supplémentaires optionnelles |
| `src/Piscine.Grading/UnitGrader.cs` | Grader `unit` : tests xUnit cachés via réflexion |
| `src/Piscine.Grading/ExerciseGradingResult.cs` | (modifié) fabrique `NotGraded` (NonCorrige) |
| `src/Piscine.Grading/ExerciseSubmission.cs` | Couple manifest + contexte pour l'orchestration |
| `src/Piscine.Grading/GroupGrader.cs` | Correction séquentielle d'un groupe, stop au 1er KO |
| `src/Piscine.Grading/ProgressRecorder.cs` | Applique les résultats à un `Progress` |
| `tests/Piscine.Grading.Tests/**` | Tests |

---

## Task 1 : Introduire `GradingContext` (refactor)

**Files:**
- Create: `src/Piscine.Grading/GradingContext.cs`
- Modify: `src/Piscine.Grading/IGrader.cs`, `IoGrader.cs`, `NormeGrader.cs`, `ExerciseGrader.cs`
- Modify: `tests/Piscine.Grading.Tests/IoGraderTests.cs`, `NormeGraderTests.cs`, `ExerciseGraderTests.cs`

- [ ] **Step 1 : Créer `GradingContext`**

`src/Piscine.Grading/GradingContext.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>Entrées d'une correction : sources livrées par la recrue + fichiers grader cachés.</summary>
public sealed class GradingContext
{
    public GradingContext(
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyDictionary<string, string>? graderFiles = null)
    {
        Sources = sources;
        GraderFiles = graderFiles ?? new Dictionary<string, string>();
    }

    /// <summary>Fichiers livrés par la recrue (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>Fichiers de notation cachés, ex. tests xUnit (nom → contenu).</summary>
    public IReadOnlyDictionary<string, string> GraderFiles { get; }
}
```

- [ ] **Step 2 : Modifier `IGrader`** pour prendre un `GradingContext`

`src/Piscine.Grading/IGrader.cs` — remplacer la signature :
```csharp
namespace Piscine.Grading;

using Piscine.Core.Model;

/// <summary>Corrige une étape de notation à partir du contexte de correction.</summary>
public interface IGrader
{
    /// <summary>Type de l'étape gérée (ex. <c>io</c>, <c>norme</c>, <c>unit</c>).</summary>
    string Type { get; }

    GraderResult Grade(GradingContext context, GradingStep step);
}
```

- [ ] **Step 3 : Modifier `IoGrader`** — signature + utilisation de `context.Sources`

Dans `src/Piscine.Grading/IoGrader.cs`, changer la signature de `Grade` et la première ligne :
```csharp
    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var compilation = CompilationService.Compile(context.Sources, OutputKind.ConsoleApplication);
```
(le reste du corps est inchangé)

- [ ] **Step 4 : Modifier `NormeGrader`** — signature + utilisation de `context.Sources`

Dans `src/Piscine.Grading/NormeGrader.cs`, changer la signature de `Grade` et la boucle :
```csharp
    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var messages = new List<string>();

        foreach (var (fileName, source) in context.Sources)
        {
```
(le reste est inchangé)

- [ ] **Step 5 : Modifier `ExerciseGrader`** — prend un `GradingContext`

Dans `src/Piscine.Grading/ExerciseGrader.cs`, remplacer la méthode `Grade` :
```csharp
    public ExerciseGradingResult Grade(ExerciseManifest manifest, GradingContext context)
    {
        var results = new List<GraderResult>();

        foreach (var step in manifest.Grading)
        {
            if (_graders.TryGetValue(step.Type, out var grader))
            {
                results.Add(grader.Grade(context, step));
            }
        }

        return new ExerciseGradingResult(manifest.Id, results);
    }
```

- [ ] **Step 6 : Mettre à jour les tests existants** pour envelopper les sources dans un `GradingContext`

Dans `tests/Piscine.Grading.Tests/IoGraderTests.cs`, remplacer les 3 appels `new IoGrader().Grade(sources, ...)` par `new IoGrader().Grade(new GradingContext(sources), ...)`.

Dans `tests/Piscine.Grading.Tests/NormeGraderTests.cs`, remplacer les 3 appels `new NormeGrader().Grade(sources, ...)` par `new NormeGrader().Grade(new GradingContext(sources), ...)`.

Dans `tests/Piscine.Grading.Tests/ExerciseGraderTests.cs`, remplacer les appels `Grader().Grade(Manifest(), sources)` et `Grader().Grade(manifest, new Dictionary<string,string>())` par des appels passant un `GradingContext` :
- `Grader().Grade(Manifest(), new GradingContext(sources))`
- `Grader().Grade(manifest, new GradingContext(new Dictionary<string, string>()))`

- [ ] **Step 7 : Lancer les tests Grading pour vérifier qu'ils repassent**

Run: `dotnet test tests/Piscine.Grading.Tests`
Expected : PASS — 15 tests (inchangés, juste adaptés).

- [ ] **Step 8 : Commit**

```bash
git add src/Piscine.Grading/GradingContext.cs src/Piscine.Grading/IGrader.cs src/Piscine.Grading/IoGrader.cs src/Piscine.Grading/NormeGrader.cs src/Piscine.Grading/ExerciseGrader.cs tests/Piscine.Grading.Tests/IoGraderTests.cs tests/Piscine.Grading.Tests/NormeGraderTests.cs tests/Piscine.Grading.Tests/ExerciseGraderTests.cs
git commit -m "refactor(grading): GradingContext (sources + fichiers grader)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : `CompilationService` — références supplémentaires

**Files:**
- Modify: `src/Piscine.Grading/CompilationService.cs`

> Les appels existants (sans références supplémentaires) restent valides via paramètre optionnel ; les tests `CompilationServiceTests` actuels couvrent ce chemin. Pas de nouveau test ici (couvert par `UnitGrader` en Task 3).

- [ ] **Step 1 : Ajouter un paramètre `additionalReferences`**

Dans `src/Piscine.Grading/CompilationService.cs`, remplacer la signature de `Compile` et la création de la compilation :
```csharp
    public static CompilationResult Compile(
        IReadOnlyDictionary<string, string> sources,
        OutputKind outputKind,
        string assemblyName = "Submission",
        IEnumerable<string>? additionalReferences = null)
    {
        var syntaxTrees = sources
            .Select(kv => CSharpSyntaxTree.ParseText(kv.Value, path: kv.Key))
            .ToList();

        var references = new List<MetadataReference>(References.Value);
        if (additionalReferences is not null)
        {
            foreach (var path in additionalReferences)
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        var options = new CSharpCompilationOptions(outputKind);
        var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, options);
```
(le reste du corps — emit, diagnostics — est inchangé)

- [ ] **Step 2 : Vérifier build + tests de compilation**

Run: `dotnet test tests/Piscine.Grading.Tests --filter CompilationServiceTests`
Expected : PASS (2 tests).

- [ ] **Step 3 : Commit**

```bash
git add src/Piscine.Grading/CompilationService.cs
git commit -m "feat(grading): CompilationService accepte des references supplementaires

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : `UnitGrader` (tests xUnit cachés via réflexion)

**Files:**
- Modify: `src/Piscine.Grading/Piscine.Grading.csproj` (packages xunit)
- Create: `src/Piscine.Grading/UnitGrader.cs`
- Test: `tests/Piscine.Grading.Tests/UnitGraderTests.cs`

- [ ] **Step 1 : Ajouter les packages xUnit à `Piscine.Grading`**

```bash
dotnet add src/Piscine.Grading package xunit.assert
dotnet add src/Piscine.Grading package xunit.core
```
Expected : deux `PackageReference` ajoutées.

- [ ] **Step 2 : Écrire les tests qui échouent**

`tests/Piscine.Grading.Tests/UnitGraderTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class UnitGraderTests
{
    private static readonly GradingStep Step = new() { Type = "unit", TestFiles = { "grader/Tests.cs" } };

    private static GradingContext Context(string learner)
    {
        var sources = new Dictionary<string, string> { ["Maths.cs"] = learner };
        var graderFiles = new Dictionary<string, string>
        {
            ["grader/Tests.cs"] = """
                using Xunit;

                public class MathsTests
                {
                    [Fact]
                    public void Add_ReturnsSum()
                    {
                        Assert.Equal(5, Maths.Add(2, 3));
                    }
                }
                """
        };
        return new GradingContext(sources, graderFiles);
    }

    [Fact]
    public void Grade_Reussi_WhenHiddenTestsPass()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a + b; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenHiddenTestsFail()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a - b; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WhenLearnerCodeDoesNotCompile()
    {
        var context = Context("public static class Maths { public static int Add(int a, int b) => a + ; }");

        var result = new UnitGrader().Grade(context, Step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("compil", System.StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 3 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter UnitGraderTests`
Expected : FAIL — `UnitGrader` introuvable.

- [ ] **Step 4 : Implémenter `UnitGrader`**

`src/Piscine.Grading/UnitGrader.cs` :
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
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

    // Force le chargement des assemblies xUnit et fournit leurs chemins comme références.
    private static readonly string[] XunitReferences =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location
    };

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
            additionalReferences: XunitReferences);

        if (!compilation.Success)
        {
            var messages = new List<string> { "Le code ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray());
        }

        return RunTests(compilation.AssemblyBytes);
    }

    private GraderResult RunTests(byte[] assemblyBytes)
    {
        var alc = new AssemblyLoadContext("unit-tests", isCollectible: true);
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var methods = FindFactMethods(assembly);

            if (methods.Count == 0)
            {
                return GraderResult.Failure(Type, "Aucun test n'a été trouvé.");
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

            if (!task.Wait(Timeout))
            {
                return GraderResult.Failure(Type, "Les tests ne se sont pas terminés à temps (boucle infinie ?).");
            }

            return failures.Count == 0
                ? GraderResult.Success(Type)
                : GraderResult.Failure(Type, failures.ToArray());
        }
        finally
        {
            alc.Unload();
        }
    }

    private static List<MethodInfo> FindFactMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetParameters().Length == 0
                && m.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "Xunit.FactAttribute"))
            .ToList();
    }

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
            var inner = ex.InnerException ?? ex;
            return inner.Message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
```

- [ ] **Step 5 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter UnitGraderTests`
Expected : PASS (3 tests).

- [ ] **Step 6 : Commit**

```bash
git add src/Piscine.Grading/Piscine.Grading.csproj src/Piscine.Grading/UnitGrader.cs tests/Piscine.Grading.Tests/UnitGraderTests.cs
git commit -m "feat(grading): UnitGrader execute les tests xUnit caches

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `ExerciseGradingResult.NotGraded` + `ExerciseSubmission` + `GroupGrader`

**Files:**
- Modify: `src/Piscine.Grading/ExerciseGradingResult.cs`
- Create: `src/Piscine.Grading/ExerciseSubmission.cs`
- Create: `src/Piscine.Grading/GroupGrader.cs`
- Test: `tests/Piscine.Grading.Tests/GroupGraderTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/GroupGraderTests.cs` :
```csharp
using System.Collections.Generic;
using System.Linq;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GroupGraderTests
{
    private static ExerciseSubmission Submission(string id, string source, string expect)
    {
        var manifest = new ExerciseManifest
        {
            Id = id,
            Grading =
            {
                new GradingStep
                {
                    Type = "io",
                    Cases = { new IoCase { ExpectStdout = expect, ExpectExit = 0 } }
                }
            }
        };
        var context = new GradingContext(new Dictionary<string, string> { ["P.cs"] = source });
        return new ExerciseSubmission(manifest, context);
    }

    private static GroupGrader Grader() => new(new ExerciseGrader(new IGrader[] { new IoGrader() }));

    [Fact]
    public void GradeGroup_StopsAtFirstFailure_MarksRestNonCorrige()
    {
        var submissions = new[]
        {
            Submission("ex00", "System.Console.Write(\"ok\");", "ok"),       // Reussi
            Submission("ex01", "System.Console.Write(\"non\");", "attendu"), // ARevoir → stop
            Submission("ex02", "System.Console.Write(\"ok\");", "ok")        // NonCorrige
        };

        var results = Grader().GradeGroup(submissions).ToList();

        Assert.Equal(GraderStatus.Reussi, results[0].Status);
        Assert.Equal(GraderStatus.ARevoir, results[1].Status);
        Assert.Equal(GraderStatus.NonCorrige, results[2].Status);
        Assert.Equal("ex02", results[2].ExerciseId);
    }

    [Fact]
    public void GradeGroup_AllReussi_WhenEveryExercisePasses()
    {
        var submissions = new[]
        {
            Submission("ex00", "System.Console.Write(\"a\");", "a"),
            Submission("ex01", "System.Console.Write(\"b\");", "b")
        };

        var results = Grader().GradeGroup(submissions).ToList();

        Assert.All(results, r => Assert.Equal(GraderStatus.Reussi, r.Status));
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GroupGraderTests`
Expected : FAIL — `ExerciseSubmission` / `GroupGrader` introuvables.

- [ ] **Step 3 : Ajouter la fabrique `NotGraded` à `ExerciseGradingResult`**

Remplacer le contenu de `src/Piscine.Grading/ExerciseGradingResult.cs` :
```csharp
using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat agrégé de la correction d'un exercice.</summary>
public sealed class ExerciseGradingResult
{
    public ExerciseGradingResult(string exerciseId, IEnumerable<GraderResult> results)
        : this(exerciseId, Aggregate(results, out var list), list)
    {
    }

    private ExerciseGradingResult(string exerciseId, GraderStatus status, IReadOnlyList<GraderResult> results)
    {
        ExerciseId = exerciseId;
        Status = status;
        Results = results;
    }

    public string ExerciseId { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<GraderResult> Results { get; }

    /// <summary>Exercice non corrigé (un exercice précédent du groupe est à revoir).</summary>
    public static ExerciseGradingResult NotGraded(string exerciseId) =>
        new(exerciseId, GraderStatus.NonCorrige, new List<GraderResult>());

    private static GraderStatus Aggregate(IEnumerable<GraderResult> results, out IReadOnlyList<GraderResult> list)
    {
        list = results.ToList();
        return list.Any(r => r.Status == GraderStatus.ARevoir)
            ? GraderStatus.ARevoir
            : GraderStatus.Reussi;
    }
}
```

- [ ] **Step 4 : Créer `ExerciseSubmission`**

`src/Piscine.Grading/ExerciseSubmission.cs` :
```csharp
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Un exercice prêt à corriger : son manifest et le contexte de correction.</summary>
public sealed class ExerciseSubmission
{
    public ExerciseSubmission(ExerciseManifest manifest, GradingContext context)
    {
        Manifest = manifest;
        Context = context;
    }

    public ExerciseManifest Manifest { get; }

    public GradingContext Context { get; }
}
```

- [ ] **Step 5 : Créer `GroupGrader`**

`src/Piscine.Grading/GroupGrader.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Grading;

/// <summary>
/// Corrige un groupe d'exercices dans l'ordre et s'arrête au premier échec :
/// les exercices suivants sont marqués <see cref="GraderStatus.NonCorrige"/>.
/// </summary>
public sealed class GroupGrader
{
    private readonly ExerciseGrader _grader;

    public GroupGrader(ExerciseGrader grader)
    {
        _grader = grader;
    }

    public IReadOnlyList<ExerciseGradingResult> GradeGroup(IEnumerable<ExerciseSubmission> submissions)
    {
        var results = new List<ExerciseGradingResult>();
        var stopped = false;

        foreach (var submission in submissions)
        {
            if (stopped)
            {
                results.Add(ExerciseGradingResult.NotGraded(submission.Manifest.Id));
                continue;
            }

            var result = _grader.Grade(submission.Manifest, submission.Context);
            results.Add(result);

            if (result.Status == GraderStatus.ARevoir)
            {
                stopped = true;
            }
        }

        return results;
    }
}
```

- [ ] **Step 6 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GroupGraderTests`
Expected : PASS (2 tests).

- [ ] **Step 7 : Commit**

```bash
git add src/Piscine.Grading/ExerciseGradingResult.cs src/Piscine.Grading/ExerciseSubmission.cs src/Piscine.Grading/GroupGrader.cs tests/Piscine.Grading.Tests/GroupGraderTests.cs
git commit -m "feat(grading): GroupGrader corrige par groupe avec stop au 1er KO

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : `ProgressRecorder` (résultats → progression)

**Files:**
- Create: `src/Piscine.Grading/ProgressRecorder.cs`
- Test: `tests/Piscine.Grading.Tests/ProgressRecorderTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/ProgressRecorderTests.cs` :
```csharp
using System;
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ProgressRecorderTests
{
    [Fact]
    public void Apply_RecordsReussiAndARevoir_AndSkipsNonCorrige()
    {
        var progress = new Progress();
        var results = new[]
        {
            new ExerciseGradingResult("ex00", new[] { GraderResult.Success("io") }),
            new ExerciseGradingResult("ex01", new[] { GraderResult.Failure("io", "KO") }),
            ExerciseGradingResult.NotGraded("ex02")
        };

        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);

        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
        Assert.Equal(ExerciseStatus.ARevoir, progress.Exercises["ex01"].Status);
        Assert.False(progress.Exercises.ContainsKey("ex02"));
        Assert.Equal(1, progress.Exercises["ex00"].Attempts);
    }

    [Fact]
    public void Apply_IncrementsAttempts_OnRepeatedGrading()
    {
        var progress = new Progress();
        var results = new[] { new ExerciseGradingResult("ex00", new[] { GraderResult.Failure("io", "KO") }) };

        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);
        ProgressRecorder.Apply(progress, results, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, progress.Exercises["ex00"].Attempts);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ProgressRecorderTests`
Expected : FAIL — `ProgressRecorder` introuvable.

- [ ] **Step 3 : Implémenter `ProgressRecorder`**

`src/Piscine.Grading/ProgressRecorder.cs` :
```csharp
using System;
using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Applique des résultats de correction à un <see cref="Progress"/> persistable.</summary>
public static class ProgressRecorder
{
    public static void Apply(Progress progress, IEnumerable<ExerciseGradingResult> results, DateTimeOffset now)
    {
        foreach (var result in results)
        {
            if (result.Status == GraderStatus.NonCorrige)
            {
                continue;
            }

            if (!progress.Exercises.TryGetValue(result.ExerciseId, out var entry))
            {
                entry = new ExerciseProgress();
                progress.Exercises[result.ExerciseId] = entry;
            }

            entry.Attempts++;
            entry.LastAttempt = now;
            entry.Status = result.Status == GraderStatus.Reussi
                ? ExerciseStatus.Reussi
                : ExerciseStatus.ARevoir;
        }
    }
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ProgressRecorderTests`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/ProgressRecorder.cs tests/Piscine.Grading.Tests/ProgressRecorderTests.cs
git commit -m "feat(grading): ProgressRecorder applique les resultats a la progression

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : Vérification globale + push

- [ ] **Step 1 : Lancer toute la suite en Release**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : PASS — tous verts (Core 13 + Grading ~25).

- [ ] **Step 2 : Pousser et vérifier la CI**

```bash
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success**.

- [ ] **Step 3 : Si la CI échoue**, `gh run view --log-failed`, corriger, commit, push, re-watch.

---

## Self-Review (effectué)

**Couverture spec (It.3) :**
- Grader `unit` (tests xUnit cachés, exécution isolée) → Task 3 (`UnitGrader`, réflexion sur `[Fact]`, ALC collectible, timeout). ✓
- Correction séquentielle par groupe, stop au 1er KO → *Non corrigé* → Task 4 (`GroupGrader` + `ExerciseGradingResult.NotGraded`). ✓
- Mise à jour de la progression → Task 5 (`ProgressRecorder`, mappe Reussi/ARevoir, ignore NonCorrige). ✓
- Accès aux fichiers grader cachés → Task 1 (`GradingContext.GraderFiles`). ✓

**Reporté à l'It.4 (CLI/intégration) :** assemblage des `ExerciseSubmission` depuis le disque (workspace + content), persistance via `ProgressStore`, fabrique `Graders.Default`.

**Placeholders :** aucun. **Cohérence des types :** `IGrader.Grade(GradingContext, GradingStep)` appliqué partout ; `GradingContext.Sources/GraderFiles` ; `CompilationService.Compile(..., additionalReferences)` ; `ExerciseGrader.Grade(ExerciseManifest, GradingContext)` ; `ExerciseGradingResult.NotGraded` + ctor public inchangé ; `GroupGrader.GradeGroup(IEnumerable<ExerciseSubmission>)` ; `ProgressRecorder.Apply(Progress, IEnumerable<ExerciseGradingResult>, DateTimeOffset)`.

**Risques :** résolution des assemblies xUnit dans l'ALC collectible via repli sur le contexte Default (xunit.assert/core chargés par `typeof(...).Assembly.Location`) ; sans timeout par test, un seul timeout global de 10 s couvre la boucle.
