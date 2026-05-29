# Itération 2 — Moulinette (compilation + io + norme) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Doter `Piscine.Grading` d'un moteur de notation : compilation Roslyn du code recrue, grader `io` (exécution isolée in-process + comparaison de sortie), grader `norme` (formatage, non bloquant), et dispatch par exercice — avec feedback éducatif.

**Architecture:** `CompilationService` compile des sources C# via Roslyn (références = `TRUSTED_PLATFORM_ASSEMBLIES`). Les graders implémentent `IGrader` (un par `type`). `IoGrader` compile en `ConsoleApplication`, charge l'assembly dans un `AssemblyLoadContext` collectible, redirige `Console` (in/out), exécute le point d'entrée sur une tâche avec timeout, compare stdout/exit attendus, et rend un `GraderResult` éducatif. `NormeGrader` compare le code au formatage canonique Roslyn (advisory, non bloquant). `ExerciseGrader` dispatche les `GradingStep` d'un manifest vers le grader correspondant et agrège en `ExerciseGradingResult`.

**Tech Stack:** .NET 10, C#, Microsoft.CodeAnalysis.CSharp (Roslyn), Microsoft.CodeAnalysis.CSharp.Workspaces (Formatter), `System.Runtime.Loader.AssemblyLoadContext`, xUnit v2.

**Contexte repo (It.0 & It.1 faites) :** `Piscine.slnx`. `Piscine.Core` contient les DTO (`ExerciseManifest`, etc.), `YamlLoader`, loaders, `ContentDiscovery`, `ProgressStore`. `Piscine.Grading` est un squelette (référence `Piscine.Core`, ne contient que `AssemblyMarker`). **Le projet `tests/Piscine.Grading.Tests` n'existe pas encore** (créé en Task 1). `Directory.Build.props` impose `Nullable=enable` + `TreatWarningsAsErrors=true` → strings initialisées à `string.Empty`, collections à `new()`. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Chaque commit finit par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

**Décisions de cadrage :** isolation **in-process** (ALC collectible + redirection Console + timeout ; un dépassement de timeout peut laisser un thread fantôme — acceptable v1, durcissement par processus séparé plus tard). Grader **`unit`** (xUnit) et **orchestration séquentielle par groupe** reportés à l'It.3. La gestion `single-file`/`TRUSTED_PLATFORM_ASSEMBLIES` au packaging est traitée à l'It.5.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Core/Model/GradingStep.cs` | Config d'une étape de notation (type + champs io/norme) + `IoCase` |
| `src/Piscine.Core/Model/FeedbackConfig.cs` | Config de feedback (`course_ref`, hints) + `FeedbackHint` |
| `src/Piscine.Core/Model/ExerciseManifest.cs` | (modifié) ajoute `Grading` et `Feedback` |
| `src/Piscine.Grading/GraderStatus.cs` | Enum du résultat transitoire (Reussi / ARevoir / NonCorrige) |
| `src/Piscine.Grading/GraderResult.cs` | Résultat d'un grader (type, statut, messages éducatifs) |
| `src/Piscine.Grading/ExerciseGradingResult.cs` | Résultat agrégé d'un exercice |
| `src/Piscine.Grading/CompilationService.cs` | Compilation Roslyn de sources → assembly + diagnostics |
| `src/Piscine.Grading/IGrader.cs` | Contrat d'un grader (`Type`, `Grade`) |
| `src/Piscine.Grading/IoGrader.cs` | Grader `io` : exécution isolée + comparaison de sortie |
| `src/Piscine.Grading/NormeGrader.cs` | Grader `norme` : formatage canonique, advisory |
| `src/Piscine.Grading/ExerciseGrader.cs` | Dispatch des étapes vers les graders, agrégation |
| `tests/Piscine.Grading.Tests/**` | Tests xUnit du moteur |

> Le grader `io` compile lui-même en `ConsoleApplication` et rapporte les erreurs de compilation comme feedback (« feedback d'erreurs de compilation » du périmètre). `NormeGrader` n'exécute aucun code.

---

## Task 1 : Projet de tests Grading + packages Roslyn

**Files:**
- Create: `tests/Piscine.Grading.Tests/Piscine.Grading.Tests.csproj`
- Modify: `src/Piscine.Grading/Piscine.Grading.csproj`

- [ ] **Step 1 : Créer le projet de tests et le câbler**

```bash
dotnet new xunit --output tests/Piscine.Grading.Tests
rm tests/Piscine.Grading.Tests/UnitTest1.cs
dotnet add tests/Piscine.Grading.Tests reference src/Piscine.Grading src/Piscine.Core
dotnet sln add tests/Piscine.Grading.Tests
```
Expected : projet créé, références ajoutées, ajouté à la solution.

- [ ] **Step 2 : Ajouter les packages Roslyn à `Piscine.Grading`**

```bash
dotnet add src/Piscine.Grading package Microsoft.CodeAnalysis.CSharp
dotnet add src/Piscine.Grading package Microsoft.CodeAnalysis.CSharp.Workspaces
```
Expected : deux `PackageReference` ajoutées.

- [ ] **Step 3 : Vérifier que tout compile**

Run: `dotnet build Piscine.slnx`
Expected : PASS — « Build succeeded », 0 erreur.

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Grading/Piscine.Grading.csproj tests/Piscine.Grading.Tests Piscine.slnx
git commit -m "chore(grading): projet de tests et packages Roslyn

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : Étendre le manifest avec grading + feedback (Core)

**Files:**
- Create: `src/Piscine.Core/Model/GradingStep.cs`
- Create: `src/Piscine.Core/Model/FeedbackConfig.cs`
- Modify: `src/Piscine.Core/Model/ExerciseManifest.cs`
- Test: `tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue** (parsing YAML d'un manifest avec grading io + norme + feedback)

`tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ManifestGradingParsingTests
{
    [Fact]
    public void Load_ParsesGradingStepsAndFeedback()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("ex00", "manifest.yaml"), """
            id: ex00-hello
            title: "Hello"
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - args: []
                    stdin: ""
                    expect_stdout: "Hello, Piscine!\n"
                    expect_exit: 0
              - type: norme
                blocking: false
            feedback:
              course_ref: "cours.md#hello-world"
              hints:
                - when: io_mismatch
                  message: "Verifie la casse et le retour a la ligne."
            """);

        var manifest = ExerciseManifestLoader.Load(dir.Combine("ex00"));

        Assert.Equal(2, manifest.Grading.Count);
        Assert.Equal("io", manifest.Grading[0].Type);
        Assert.Single(manifest.Grading[0].Cases);
        Assert.Equal("Hello, Piscine!\n", manifest.Grading[0].Cases[0].ExpectStdout);
        Assert.Equal(0, manifest.Grading[0].Cases[0].ExpectExit);
        Assert.Equal("norme", manifest.Grading[1].Type);
        Assert.False(manifest.Grading[1].Blocking);
        Assert.Equal("cours.md#hello-world", manifest.Feedback.CourseRef);
        Assert.Equal("io_mismatch", manifest.Feedback.Hints[0].When);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter ManifestGradingParsingTests`
Expected : FAIL — compilation : `ExerciseManifest` n'a pas de membre `Grading`/`Feedback`.

- [ ] **Step 3 : Créer `GradingStep` et `IoCase`**

`src/Piscine.Core/Model/GradingStep.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Une étape de notation déclarée dans le manifest (type io / unit / norme).</summary>
public sealed class GradingStep
{
    public string Type { get; set; } = string.Empty;

    /// <summary>Cas d'exécution pour le grader <c>io</c>.</summary>
    public List<IoCase> Cases { get; set; } = new();

    /// <summary>Fichiers de tests cachés pour le grader <c>unit</c> (consommé à l'It.3).</summary>
    public List<string> TestFiles { get; set; } = new();

    /// <summary>Pour le grader <c>norme</c> : si vrai, un écart de norme fait échouer l'exercice.</summary>
    public bool Blocking { get; set; }
}

/// <summary>Un cas d'exécution pour le grader <c>io</c>.</summary>
public sealed class IoCase
{
    public List<string> Args { get; set; } = new();

    public string Stdin { get; set; } = string.Empty;

    public string ExpectStdout { get; set; } = string.Empty;

    public int ExpectExit { get; set; }
}
```

- [ ] **Step 4 : Créer `FeedbackConfig` et `FeedbackHint`**

`src/Piscine.Core/Model/FeedbackConfig.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Configuration de feedback éducatif d'un exercice.</summary>
public sealed class FeedbackConfig
{
    /// <summary>Ancre vers la section de cours pertinente, ex. <c>cours.md#hello-world</c>.</summary>
    public string CourseRef { get; set; } = string.Empty;

    public List<FeedbackHint> Hints { get; set; } = new();
}

/// <summary>Un indice conditionnel affiché selon un déclencheur.</summary>
public sealed class FeedbackHint
{
    /// <summary>Déclencheur, ex. <c>io_mismatch</c>.</summary>
    public string When { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
```

- [ ] **Step 5 : Modifier `ExerciseManifest`** pour ajouter les deux sections

Dans `src/Piscine.Core/Model/ExerciseManifest.cs`, ajouter après la propriété `Starter` :
```csharp
    public List<GradingStep> Grading { get; set; } = new();

    public FeedbackConfig Feedback { get; set; } = new();
```

- [ ] **Step 6 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter ManifestGradingParsingTests`
Expected : PASS.

- [ ] **Step 7 : Commit**

```bash
git add src/Piscine.Core/Model/GradingStep.cs src/Piscine.Core/Model/FeedbackConfig.cs src/Piscine.Core/Model/ExerciseManifest.cs tests/Piscine.Core.Tests/ManifestGradingParsingTests.cs
git commit -m "feat(core): manifest porte la config grading et feedback

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : Modèle de résultat (`GraderStatus`, `GraderResult`, `ExerciseGradingResult`)

**Files:**
- Create: `src/Piscine.Grading/GraderStatus.cs`
- Create: `src/Piscine.Grading/GraderResult.cs`
- Create: `src/Piscine.Grading/ExerciseGradingResult.cs`
- Test: `tests/Piscine.Grading.Tests/GraderResultTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/GraderResultTests.cs` :
```csharp
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GraderResultTests
{
    [Fact]
    public void Success_HasReussiStatus()
    {
        var result = GraderResult.Success("io");

        Assert.Equal("io", result.GraderType);
        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Failure_HasARevoirStatusAndMessages()
    {
        var result = GraderResult.Failure("io", "Sortie inattendue.");

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(new[] { "Sortie inattendue." }, result.Messages);
    }

    [Fact]
    public void Aggregate_IsARevoir_WhenAnyResultIsARevoir()
    {
        var aggregate = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Success("norme"),
            GraderResult.Failure("io", "KO")
        });

        Assert.Equal("ex00", aggregate.ExerciseId);
        Assert.Equal(GraderStatus.ARevoir, aggregate.Status);
    }

    [Fact]
    public void Aggregate_IsReussi_WhenAllResultsReussi()
    {
        var aggregate = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Success("io"),
            GraderResult.Success("norme")
        });

        Assert.Equal(GraderStatus.Reussi, aggregate.Status);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GraderResultTests`
Expected : FAIL — types `GraderResult` / `GraderStatus` / `ExerciseGradingResult` introuvables.

- [ ] **Step 3 : Créer `GraderStatus`**

`src/Piscine.Grading/GraderStatus.cs` :
```csharp
namespace Piscine.Grading;

/// <summary>Résultat transitoire d'une correction (non persisté tel quel).</summary>
public enum GraderStatus
{
    Reussi,
    ARevoir,
    NonCorrige
}
```

- [ ] **Step 4 : Créer `GraderResult`**

`src/Piscine.Grading/GraderResult.cs` :
```csharp
using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat éducatif produit par un grader pour une étape de notation.</summary>
public sealed class GraderResult
{
    private GraderResult(string graderType, GraderStatus status, IReadOnlyList<string> messages)
    {
        GraderType = graderType;
        Status = status;
        Messages = messages;
    }

    public string GraderType { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<string> Messages { get; }

    public static GraderResult Success(string graderType) =>
        new(graderType, GraderStatus.Reussi, new List<string>());

    public static GraderResult Failure(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.ARevoir, messages.ToList());

    /// <summary>Réussite avec messages consultatifs (ex. norme non bloquante).</summary>
    public static GraderResult Advisory(string graderType, params string[] messages) =>
        new(graderType, GraderStatus.Reussi, messages.ToList());
}
```

- [ ] **Step 5 : Créer `ExerciseGradingResult`**

`src/Piscine.Grading/ExerciseGradingResult.cs` :
```csharp
using System.Collections.Generic;
using System.Linq;

namespace Piscine.Grading;

/// <summary>Résultat agrégé de la correction d'un exercice.</summary>
public sealed class ExerciseGradingResult
{
    public ExerciseGradingResult(string exerciseId, IEnumerable<GraderResult> results)
    {
        ExerciseId = exerciseId;
        Results = results.ToList();
        Status = Results.Any(r => r.Status == GraderStatus.ARevoir)
            ? GraderStatus.ARevoir
            : GraderStatus.Reussi;
    }

    public string ExerciseId { get; }

    public GraderStatus Status { get; }

    public IReadOnlyList<GraderResult> Results { get; }
}
```

- [ ] **Step 6 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GraderResultTests`
Expected : PASS (4 tests).

- [ ] **Step 7 : Commit**

```bash
git add src/Piscine.Grading/GraderStatus.cs src/Piscine.Grading/GraderResult.cs src/Piscine.Grading/ExerciseGradingResult.cs tests/Piscine.Grading.Tests/GraderResultTests.cs
git commit -m "feat(grading): modele de resultat de correction

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `CompilationService` (Roslyn)

**Files:**
- Create: `src/Piscine.Grading/CompilationService.cs`
- Test: `tests/Piscine.Grading.Tests/CompilationServiceTests.cs`

- [ ] **Step 1 : Écrire les tests qui échouent** (compile OK + compile KO avec diagnostics)

`tests/Piscine.Grading.Tests/CompilationServiceTests.cs` :
```csharp
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class CompilationServiceTests
{
    [Fact]
    public void Compile_Succeeds_OnValidConsoleProgram()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.WriteLine("Hi");
                """
        };

        var result = CompilationService.Compile(sources, OutputKind.ConsoleApplication);

        Assert.True(result.Success);
        Assert.NotEmpty(result.AssemblyBytes);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Compile_Fails_AndReportsErrors_OnSyntaxError()
    {
        var sources = new Dictionary<string, string>
        {
            ["Bad.cs"] = "this is not valid C#"
        };

        var result = CompilationService.Compile(sources, OutputKind.ConsoleApplication);

        Assert.False(result.Success);
        Assert.Empty(result.AssemblyBytes);
        Assert.NotEmpty(result.Errors);
    }
}
```

- [ ] **Step 2 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter CompilationServiceTests`
Expected : FAIL — `CompilationService` introuvable.

- [ ] **Step 3 : Implémenter `CompilationService`**

`src/Piscine.Grading/CompilationService.cs` :
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Piscine.Grading;

/// <summary>Résultat d'une compilation Roslyn.</summary>
public sealed class CompilationResult
{
    private CompilationResult(bool success, byte[] assemblyBytes, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        Success = success;
        AssemblyBytes = assemblyBytes;
        Errors = errors;
        Warnings = warnings;
    }

    public bool Success { get; }

    public byte[] AssemblyBytes { get; }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static CompilationResult Ok(byte[] bytes, IReadOnlyList<string> warnings) =>
        new(true, bytes, new List<string>(), warnings);

    public static CompilationResult Failed(IReadOnlyList<string> errors) =>
        new(false, Array.Empty<byte>(), errors, new List<string>());
}

/// <summary>Compile des sources C# en mémoire via Roslyn.</summary>
public static class CompilationService
{
    public static CompilationResult Compile(
        IReadOnlyDictionary<string, string> sources,
        OutputKind outputKind,
        string assemblyName = "Submission")
    {
        var syntaxTrees = sources
            .Select(kv => CSharpSyntaxTree.ParseText(kv.Value, path: kv.Key))
            .ToList();

        var options = new CSharpCompilationOptions(outputKind);
        var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, References.Value, options);

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);

        if (!emit.Success)
        {
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(Format)
                .ToList();
            return CompilationResult.Failed(errors);
        }

        var warnings = emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Warning)
            .Select(Format)
            .ToList();
        return CompilationResult.Ok(ms.ToArray(), warnings);
    }

    private static string Format(Diagnostic diagnostic)
    {
        var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
        return $"ligne {line} : {diagnostic.GetMessage()}";
    }

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(LoadReferences);

    private static IReadOnlyList<MetadataReference> LoadReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
    }
}
```

- [ ] **Step 4 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter CompilationServiceTests`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/CompilationService.cs tests/Piscine.Grading.Tests/CompilationServiceTests.cs
git commit -m "feat(grading): CompilationService compile via Roslyn

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : Contrat `IGrader`

**Files:**
- Create: `src/Piscine.Grading/IGrader.cs`

> Pas de test dédié (interface) ; couverte par les graders concrets (Tasks 6-7). Vérification de compilation.

- [ ] **Step 1 : Créer `IGrader`**

`src/Piscine.Grading/IGrader.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Corrige une étape de notation à partir des sources livrées par la recrue.</summary>
public interface IGrader
{
    /// <summary>Type de l'étape gérée (ex. <c>io</c>, <c>norme</c>).</summary>
    string Type { get; }

    GraderResult Grade(IReadOnlyDictionary<string, string> sources, GradingStep step);
}
```

- [ ] **Step 2 : Vérifier la compilation**

Run: `dotnet build src/Piscine.Grading`
Expected : PASS — 0 erreur.

- [ ] **Step 3 : Commit**

```bash
git add src/Piscine.Grading/IGrader.cs
git commit -m "feat(grading): contrat IGrader

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : `IoGrader` (exécution isolée + comparaison)

**Files:**
- Create: `src/Piscine.Grading/IoGrader.cs`
- Test: `tests/Piscine.Grading.Tests/IoGraderTests.cs`

- [ ] **Step 1 : Écrire les tests qui échouent** (programme correct → Reussi ; sortie fausse → ARevoir ; erreur de compilation → ARevoir)

`tests/Piscine.Grading.Tests/IoGraderTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class IoGraderTests
{
    private static GradingStep IoStep(string expectStdout, int expectExit = 0)
    {
        return new GradingStep
        {
            Type = "io",
            Cases = { new IoCase { ExpectStdout = expectStdout, ExpectExit = expectExit } }
        };
    }

    [Fact]
    public void Grade_Reussi_WhenStdoutMatches()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Hello, Piscine!");
                """
        };

        var result = new IoGrader().Grade(sources, IoStep("Hello, Piscine!"));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenStdoutDiffers()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.Write("Bonjour");
                """
        };

        var result = new IoGrader().Grade(sources, IoStep("Hello, Piscine!"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WithCompileErrors()
    {
        var sources = new Dictionary<string, string>
        {
            ["Bad.cs"] = "ceci ne compile pas"
        };

        var result = new IoGrader().Grade(sources, IoStep("peu importe"));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("compil", System.StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter IoGraderTests`
Expected : FAIL — `IoGrader` introuvable.

- [ ] **Step 3 : Implémenter `IoGrader`**

`src/Piscine.Grading/IoGrader.cs` :
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
/// Grader <c>io</c> : compile le programme de la recrue, l'exécute dans un contexte isolé,
/// et compare la sortie standard / le code de sortie aux attentes.
/// </summary>
public sealed class IoGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Type => "io";

    public GraderResult Grade(IReadOnlyDictionary<string, string> sources, GradingStep step)
    {
        var compilation = CompilationService.Compile(sources, OutputKind.ConsoleApplication);
        if (!compilation.Success)
        {
            var messages = new List<string> { "Le programme ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray());
        }

        foreach (var ioCase in step.Cases)
        {
            var run = Execute(compilation.AssemblyBytes, ioCase.Args.ToArray(), ioCase.Stdin);

            if (run.TimedOut)
            {
                return GraderResult.Failure(Type, "Votre programme ne s'est pas terminé à temps (boucle infinie ?).");
            }

            if (run.Error is not null)
            {
                return GraderResult.Failure(Type, $"Votre programme a levé une exception : {run.Error.GetType().Name} — {run.Error.Message}");
            }

            if (Normalize(run.Stdout) != Normalize(ioCase.ExpectStdout))
            {
                return GraderResult.Failure(
                    Type,
                    "La sortie ne correspond pas.",
                    $"Attendu : {Quote(ioCase.ExpectStdout)}",
                    $"Obtenu  : {Quote(run.Stdout)}");
            }

            if (run.ExitCode != ioCase.ExpectExit)
            {
                return GraderResult.Failure(
                    Type,
                    $"Code de sortie inattendu : attendu {ioCase.ExpectExit}, obtenu {run.ExitCode}.");
            }
        }

        return GraderResult.Success(Type);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\n", "\\n") + "\"";

    private sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, Exception? Error);

    private static RunOutcome Execute(byte[] assemblyBytes, string[] args, string stdin)
    {
        var alc = new AssemblyLoadContext("submission", isCollectible: true);
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var output = new StringWriter();
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var entry = assembly.EntryPoint;
            if (entry is null)
            {
                return new RunOutcome(string.Empty, 0, false, new InvalidOperationException("Aucun point d'entrée (Main)."));
            }

            Console.SetOut(output);
            Console.SetIn(new StringReader(stdin));

            int exitCode = 0;
            Exception? error = null;
            var task = Task.Run(() =>
            {
                try
                {
                    exitCode = InvokeEntry(entry, args);
                }
                catch (TargetInvocationException ex)
                {
                    error = ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            if (!task.Wait(Timeout))
            {
                return new RunOutcome(output.ToString(), 0, true, null);
            }

            return new RunOutcome(output.ToString(), exitCode, false, error);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            alc.Unload();
        }
    }

    private static int InvokeEntry(MethodInfo entry, string[] args)
    {
        var invokeArgs = entry.GetParameters().Length == 1
            ? new object[] { args }
            : Array.Empty<object>();

        var result = entry.Invoke(null, invokeArgs);
        return result switch
        {
            int code => code,
            Task<int> taskInt => taskInt.GetAwaiter().GetResult(),
            Task task => Await(task),
            _ => 0
        };
    }

    private static int Await(Task task)
    {
        task.GetAwaiter().GetResult();
        return 0;
    }
}
```

- [ ] **Step 4 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter IoGraderTests`
Expected : PASS (3 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/IoGrader.cs tests/Piscine.Grading.Tests/IoGraderTests.cs
git commit -m "feat(grading): IoGrader execute et compare la sortie

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7 : `NormeGrader` (formatage, non bloquant)

**Files:**
- Create: `src/Piscine.Grading/NormeGrader.cs`
- Test: `tests/Piscine.Grading.Tests/NormeGraderTests.cs`

- [ ] **Step 1 : Écrire les tests qui échouent** (code mal formaté → advisory présent ; code canonique → aucun message)

`tests/Piscine.Grading.Tests/NormeGraderTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class NormeGraderTests
{
    private static readonly GradingStep Step = new() { Type = "norme", Blocking = false };

    [Fact]
    public void Grade_Advisory_OnPoorlyFormattedCode()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A{void M(){int x=1;}}"
        };

        var result = new NormeGrader().Grade(sources, Step);

        Assert.Equal(GraderStatus.Reussi, result.Status); // non bloquant
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void Grade_NoMessages_OnCanonicalCode()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A\n{\n    void M()\n    {\n        int x = 1;\n    }\n}\n"
        };

        var result = new NormeGrader().Grade(sources, Step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Grade_ARevoir_WhenBlockingAndPoorlyFormatted()
    {
        var sources = new Dictionary<string, string>
        {
            ["A.cs"] = "class A{void M(){int x=1;}}"
        };

        var result = new NormeGrader().Grade(sources, new GradingStep { Type = "norme", Blocking = true });

        Assert.Equal(GraderStatus.ARevoir, result.Status);
    }
}
```

- [ ] **Step 2 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter NormeGraderTests`
Expected : FAIL — `NormeGrader` introuvable.

- [ ] **Step 3 : Implémenter `NormeGrader`**

`src/Piscine.Grading/NormeGrader.cs` :
```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>norme</c> : compare chaque fichier au formatage canonique Roslyn.
/// Non bloquant par défaut (advisory) ; bloquant si <see cref="GradingStep.Blocking"/>.
/// </summary>
public sealed class NormeGrader : IGrader
{
    public string Type => "norme";

    public GraderResult Grade(IReadOnlyDictionary<string, string> sources, GradingStep step)
    {
        var messages = new List<string>();

        foreach (var (fileName, source) in sources)
        {
            if (!IsCanonical(source))
            {
                messages.Add($"{fileName} : le formatage diffère de la norme (indentation, espaces, accolades).");
            }
        }

        if (messages.Count == 0)
        {
            return GraderResult.Success(Type);
        }

        return step.Blocking
            ? GraderResult.Failure(Type, messages.ToArray())
            : GraderResult.Advisory(Type, messages.ToArray());
    }

    private static bool IsCanonical(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        using var workspace = new AdhocWorkspace();
        var formatted = Formatter.Format(root, workspace).ToFullString();
        return Normalize(formatted) == Normalize(source);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd();
}
```

- [ ] **Step 4 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter NormeGraderTests`
Expected : PASS (3 tests).

> Si le test `Grade_NoMessages_OnCanonicalCode` échoue parce que `Formatter` reformate malgré tout l'échantillon, ajuster l'échantillon « canonique » du test à la sortie exacte de `Formatter` (le comportement de référence prime). Ne pas affaiblir l'assertion du cas mal formaté.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/NormeGrader.cs tests/Piscine.Grading.Tests/NormeGraderTests.cs
git commit -m "feat(grading): NormeGrader verifie le formatage canonique

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8 : `ExerciseGrader` (dispatch + agrégation)

**Files:**
- Create: `src/Piscine.Grading/ExerciseGrader.cs`
- Test: `tests/Piscine.Grading.Tests/ExerciseGraderTests.cs`

- [ ] **Step 1 : Écrire les tests qui échouent** (manifest io+norme : sources correctes → Reussi ; sortie fausse → ARevoir)

`tests/Piscine.Grading.Tests/ExerciseGraderTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ExerciseGraderTests
{
    private static ExerciseManifest Manifest()
    {
        return new ExerciseManifest
        {
            Id = "ex00-hello",
            Grading =
            {
                new GradingStep
                {
                    Type = "io",
                    Cases = { new IoCase { ExpectStdout = "Hello, Piscine!", ExpectExit = 0 } }
                },
                new GradingStep { Type = "norme", Blocking = false }
            }
        };
    }

    private static ExerciseGrader Grader() => new(new IGrader[] { new IoGrader(), new NormeGrader() });

    [Fact]
    public void Grade_Reussi_OnCorrectSubmission()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = "System.Console.Write(\"Hello, Piscine!\");"
        };

        var result = Grader().Grade(Manifest(), sources);

        Assert.Equal("ex00-hello", result.ExerciseId);
        Assert.Equal(GraderStatus.Reussi, result.Status);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public void Grade_ARevoir_OnWrongOutput()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = "System.Console.Write(\"Nope\");"
        };

        var result = Grader().Grade(Manifest(), sources);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
    }

    [Fact]
    public void Grade_SkipsUnknownGraderTypes()
    {
        var manifest = new ExerciseManifest
        {
            Id = "ex01",
            Grading = { new GradingStep { Type = "unit", TestFiles = { "grader/Tests.cs" } } }
        };

        var result = Grader().Grade(manifest, new Dictionary<string, string>());

        // 'unit' n'est pas encore enregistré (It.3) : aucune étape corrigée → Reussi par défaut.
        Assert.Empty(result.Results);
        Assert.Equal(GraderStatus.Reussi, result.Status);
    }
}
```

- [ ] **Step 2 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ExerciseGraderTests`
Expected : FAIL — `ExerciseGrader` introuvable.

- [ ] **Step 3 : Implémenter `ExerciseGrader`**

`src/Piscine.Grading/ExerciseGrader.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Corrige un exercice en dispatchant ses étapes vers les graders enregistrés.</summary>
public sealed class ExerciseGrader
{
    private readonly Dictionary<string, IGrader> _graders = new();

    public ExerciseGrader(IEnumerable<IGrader> graders)
    {
        foreach (var grader in graders)
        {
            _graders[grader.Type] = grader;
        }
    }

    public ExerciseGradingResult Grade(ExerciseManifest manifest, IReadOnlyDictionary<string, string> sources)
    {
        var results = new List<GraderResult>();

        foreach (var step in manifest.Grading)
        {
            if (_graders.TryGetValue(step.Type, out var grader))
            {
                results.Add(grader.Grade(sources, step));
            }
        }

        return new ExerciseGradingResult(manifest.Id, results);
    }
}
```

- [ ] **Step 4 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ExerciseGraderTests`
Expected : PASS (3 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/ExerciseGrader.cs tests/Piscine.Grading.Tests/ExerciseGraderTests.cs
git commit -m "feat(grading): ExerciseGrader dispatche et agrege les resultats

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 9 : Vérification globale + push

**Files:** aucun (vérification)

- [ ] **Step 1 : Lancer toute la suite en Release (comme la CI)**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : PASS — tous les tests verts (12 antérieurs + ~15 de l'It.2).

- [ ] **Step 2 : Pousser sur main et vérifier la CI**

```bash
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success**.

- [ ] **Step 3 : Si la CI échoue**, diagnostiquer puis corriger

```bash
gh run view --log-failed
```
Corriger localement, commit, push, relancer `gh run watch --exit-status` jusqu'au vert.

---

## Self-Review (effectué)

**Couverture spec (It.2, périmètre scindé) :**
- Compilation Roslyn → Task 4 (`CompilationService`, références TPA). ✓
- Grader `io` (exécution isolée in-process + comparaison stdout/exit + feedback diff) → Task 6 (`IoGrader`). ✓
- Feedback d'erreurs de compilation → intégré au Task 6 (compile KO → ARevoir avec diagnostics). ✓
- Grader `norme` (formatage, non bloquant, advisory ; bloquant si configuré) → Task 7 (`NormeGrader`). ✓
- Config grading/feedback dans le manifest → Task 2 (extension Core). ✓
- Dispatch par exercice → Task 8 (`ExerciseGrader`). ✓
- Grader `unit` + orchestration séquentielle par groupe → **explicitement reportés à l'It.3** (documenté ; Task 8 ignore proprement le type `unit`).

**Placeholders :** aucun TODO/TBD ; code et commandes complets. Le `TestFiles` de `GradingStep` est défini dès maintenant (consommé en It.3) pour éviter une rupture de modèle.

**Cohérence des types :** `GradingStep.Type/Cases/Blocking/TestFiles`, `IoCase.ExpectStdout/ExpectExit/Args/Stdin`, `FeedbackConfig.CourseRef/Hints`, `GraderResult.Success/Failure/Advisory` + `.GraderType/.Status/.Messages`, `GraderStatus.{Reussi,ARevoir,NonCorrige}`, `ExerciseGradingResult(string, IEnumerable<GraderResult>)` + `.ExerciseId/.Status/.Results`, `CompilationService.Compile(IReadOnlyDictionary<string,string>, OutputKind, string)` → `CompilationResult.Success/AssemblyBytes/Errors/Warnings`, `IGrader.Type/Grade(IReadOnlyDictionary<string,string>, GradingStep)` : signatures et noms cohérents entre tests et implémentations.

**Nullable / WarningsAsErrors :** DTO initialisés ; `RunOutcome.Error` est `Exception?` ; `AppContext.GetData` casté en `string?` avec repli `string.Empty`.

**Risque identifié :** le test `norme` « code canonique » dépend du comportement exact de `Formatter` (note de repli au Task 7, Step 4). L'isolation in-process ne protège pas d'un thread fantôme sur timeout (documenté ; durcissement futur).
