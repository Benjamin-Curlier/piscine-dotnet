# Itération 4 — CLI UX (boucle locale `check`) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Cases `- [ ]` pour le suivi.

**Goal:** Rendre la piscine utilisable en local : commandes `list`, `start <exo>`, `check <exo>`, `status`, qui assemblent les soumissions depuis le disque, corrigent, affichent un feedback éducatif et persistent la progression.

**Architecture:** Composants purs/testables dans `Piscine.Core` (localisation de contenu, installation de starter, résolution des chemins) et `Piscine.Grading` (assemblage de soumission, formatage de feedback, commande `check` intégrée + fabrique `Graders.Default`). `Piscine.Cli/Program.cs` ne fait que router les arguments vers ces composants. Tout est testé sur des dossiers temporaires ; le `git push`→moulinette vient à l'It.5.

**Tech Stack:** .NET 10, xUnit. Aucune nouvelle dépendance.

**Contexte repo (It.0→It.3 faites) :** moteur de notation complet dans `Piscine.Grading` (`ExerciseGrader`, `IoGrader`/`NormeGrader`/`UnitGrader`, `GroupGrader`, `ProgressRecorder`, `GradingContext`, `ExerciseSubmission`). `Piscine.Core` : `ExerciseManifest`/`Module`, `ModuleLoader`/`ExerciseManifestLoader`, `ContentDiscovery`, `PiscinePaths`, `ProgressStore`, `Progress`. `Piscine.Cli` affiche juste une bannière. `Directory.Build.props` : Nullable + TreatWarningsAsErrors. `tests/Piscine.Core.Tests` a l'utilitaire `TempDir`. **`tests/Piscine.Grading.Tests` n'a PAS `TempDir`** (créé en Task 5). Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Grading/Graders.cs` | Fabrique `Graders.Default()` → `ExerciseGrader` (io+norme+unit) |
| `src/Piscine.Core/Content/ExerciseLocation.cs` | Emplacement d'un exercice (module, dossier contenu) |
| `src/Piscine.Core/Content/ContentLocator.cs` | Retrouve un exercice par id en scannant le contenu |
| `src/Piscine.Core/Content/StarterInstaller.cs` | Copie les fichiers `starter/` vers le workspace |
| `src/Piscine.Core/PiscineLayout.cs` | Résout content / workspace / état (env + conventions) |
| `src/Piscine.Grading/SubmissionLoader.cs` | Assemble une `ExerciseSubmission` depuis le disque |
| `src/Piscine.Grading/ResultFormatter.cs` | Rend un `ExerciseGradingResult` en texte éducatif |
| `src/Piscine.Grading/CheckCommand.cs` | Localise → assemble → corrige → persiste → formate |
| `src/Piscine.Cli/Program.cs` | (modifié) routage des commandes |
| `tests/Piscine.Grading.Tests/TempDir.cs` | Utilitaire de test (copie depuis Core.Tests) |

---

## Task 1 : Fabrique `Graders.Default()`

**Files:**
- Create: `src/Piscine.Grading/Graders.cs`
- Test: `tests/Piscine.Grading.Tests/GradersTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/GradersTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class GradersTests
{
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
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GradersTests`
Expected : FAIL — `Graders` introuvable.

- [ ] **Step 3 : Implémenter `Graders`**

`src/Piscine.Grading/Graders.cs` :
```csharp
namespace Piscine.Grading;

/// <summary>Fabrique l'ensemble standard de graders de la piscine.</summary>
public static class Graders
{
    public static ExerciseGrader Default() =>
        new(new IGrader[] { new IoGrader(), new NormeGrader(), new UnitGrader() });
}
```

- [ ] **Step 4 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter GradersTests`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/Graders.cs tests/Piscine.Grading.Tests/GradersTests.cs
git commit -m "feat(grading): fabrique Graders.Default (io+norme+unit)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : `ContentLocator`

**Files:**
- Create: `src/Piscine.Core/Content/ExerciseLocation.cs`
- Create: `src/Piscine.Core/Content/ContentLocator.cs`
- Test: `tests/Piscine.Core.Tests/ContentLocatorTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/ContentLocatorTests.cs` :
```csharp
using System.IO;
using Piscine.Core;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentLocatorTests
{
    [Fact]
    public void FindExercise_ReturnsLocation_WhenExerciseExists()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");
        dir.WriteFile(Path.Combine("modules", "00-setup", "exercises", "ex00-hello", "manifest.yaml"), "id: ex00-hello\n");

        var location = ContentLocator.FindExercise(new PiscinePaths(dir.Path), "ex00-hello");

        Assert.NotNull(location);
        Assert.Equal("00-setup", location!.ModuleId);
        Assert.Equal("ex00-hello", location.ExerciseId);
        Assert.True(Directory.Exists(location.ContentDir));
    }

    [Fact]
    public void FindExercise_ReturnsNull_WhenMissing()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");

        var location = ContentLocator.FindExercise(new PiscinePaths(dir.Path), "inconnu");

        Assert.Null(location);
    }
}
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Core.Tests --filter ContentLocatorTests`
Expected : FAIL — types introuvables.

- [ ] **Step 3 : Créer `ExerciseLocation`**

`src/Piscine.Core/Content/ExerciseLocation.cs` :
```csharp
namespace Piscine.Core.Content;

/// <summary>Emplacement d'un exercice dans le contenu.</summary>
public sealed record ExerciseLocation(string ModuleId, string ExerciseId, string ContentDir);
```

- [ ] **Step 4 : Créer `ContentLocator`**

`src/Piscine.Core/Content/ContentLocator.cs` :
```csharp
using System.IO;
using System.Linq;

namespace Piscine.Core.Content;

/// <summary>Retrouve un exercice par identifiant en scannant les modules.</summary>
public static class ContentLocator
{
    public const string ExercisesDirName = "exercises";

    public static ExerciseLocation? FindExercise(PiscinePaths content, string exerciseId)
    {
        if (!Directory.Exists(content.ModulesDirectory))
        {
            return null;
        }

        foreach (var moduleDir in Directory.EnumerateDirectories(content.ModulesDirectory))
        {
            var exerciseDir = Path.Combine(moduleDir, ExercisesDirName, exerciseId);
            var manifestPath = Path.Combine(exerciseDir, ExerciseManifestLoader.FileName);
            var moduleManifest = Path.Combine(moduleDir, ModuleLoader.FileName);

            if (File.Exists(manifestPath) && File.Exists(moduleManifest))
            {
                var module = ModuleLoader.Load(moduleDir);
                return new ExerciseLocation(module.Id, exerciseId, exerciseDir);
            }
        }

        return null;
    }
}
```

- [ ] **Step 5 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Core.Tests --filter ContentLocatorTests`
Expected : PASS (2 tests).

- [ ] **Step 6 : Commit**

```bash
git add src/Piscine.Core/Content/ExerciseLocation.cs src/Piscine.Core/Content/ContentLocator.cs tests/Piscine.Core.Tests/ContentLocatorTests.cs
git commit -m "feat(core): ContentLocator retrouve un exercice par id

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : `StarterInstaller`

**Files:**
- Create: `src/Piscine.Core/Content/StarterInstaller.cs`
- Test: `tests/Piscine.Core.Tests/StarterInstallerTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/StarterInstallerTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class StarterInstallerTests
{
    [Fact]
    public void Install_CopiesStarterFiles_WithoutOverwritingLearnerWork()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "starter", "README.md"), "consigne");
        dir.WriteFile(Path.Combine("content", "starter", "Hello.cs"), "// squelette");
        // La recrue a déjà commencé Hello.cs : ne pas l'écraser.
        dir.WriteFile(Path.Combine("ws", "Hello.cs"), "mon travail");

        StarterInstaller.Install(dir.Combine("content"), dir.Combine("ws"));

        Assert.Equal("consigne", File.ReadAllText(dir.Combine(Path.Combine("ws", "README.md"))));
        Assert.Equal("mon travail", File.ReadAllText(dir.Combine(Path.Combine("ws", "Hello.cs"))));
    }

    [Fact]
    public void Install_NoStarterDir_DoesNothing()
    {
        using var dir = new TempDir();
        Directory.CreateDirectory(dir.Combine("content"));

        StarterInstaller.Install(dir.Combine("content"), dir.Combine("ws"));

        Assert.True(Directory.Exists(dir.Combine("ws")));
    }
}
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Core.Tests --filter StarterInstallerTests`
Expected : FAIL — `StarterInstaller` introuvable.

- [ ] **Step 3 : Implémenter `StarterInstaller`**

`src/Piscine.Core/Content/StarterInstaller.cs` :
```csharp
using System.IO;

namespace Piscine.Core.Content;

/// <summary>Copie les fichiers du dossier <c>starter/</c> d'un exercice vers le workspace.</summary>
public static class StarterInstaller
{
    public const string StarterDirName = "starter";

    public static void Install(string exerciseContentDir, string workspaceExerciseDir)
    {
        Directory.CreateDirectory(workspaceExerciseDir);

        var starterDir = Path.Combine(exerciseContentDir, StarterDirName);
        if (!Directory.Exists(starterDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(starterDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(starterDir, file);
            var destination = Path.Combine(workspaceExerciseDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            if (!File.Exists(destination))
            {
                File.Copy(file, destination);
            }
        }
    }
}
```

- [ ] **Step 4 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Core.Tests --filter StarterInstallerTests`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Content/StarterInstaller.cs tests/Piscine.Core.Tests/StarterInstallerTests.cs
git commit -m "feat(core): StarterInstaller copie les fichiers starter

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `PiscineLayout`

**Files:**
- Create: `src/Piscine.Core/PiscineLayout.cs`
- Test: `tests/Piscine.Core.Tests/PiscineLayoutTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/PiscineLayoutTests.cs` :
```csharp
using System.IO;
using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class PiscineLayoutTests
{
    [Fact]
    public void Layout_ExposesDerivedPaths()
    {
        var layout = new PiscineLayout("/c", "/home/ws", "/home/.state");

        Assert.Equal("/c", layout.ContentRoot);
        Assert.Equal(Path.Combine("/c", "modules"), layout.Content.ModulesDirectory);
        Assert.Equal("/home/ws", layout.WorkspaceRoot);
        Assert.Equal(Path.Combine("/home/.state", "progress.json"), layout.ProgressPath);
    }

    [Fact]
    public void WorkspaceExerciseDir_CombinesModuleAndExercise()
    {
        var layout = new PiscineLayout("/c", "/ws", "/s");

        Assert.Equal(
            Path.Combine("/ws", "00-setup", "ex00"),
            layout.WorkspaceExerciseDir("00-setup", "ex00"));
    }
}
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Core.Tests --filter PiscineLayoutTests`
Expected : FAIL — `PiscineLayout` introuvable.

- [ ] **Step 3 : Implémenter `PiscineLayout`**

`src/Piscine.Core/PiscineLayout.cs` :
```csharp
using System;
using System.IO;

namespace Piscine.Core;

/// <summary>Résout les emplacements de la piscine : contenu, workspace, état persistant.</summary>
public sealed class PiscineLayout
{
    public PiscineLayout(string contentRoot, string workspaceRoot, string stateDir)
    {
        ContentRoot = contentRoot;
        WorkspaceRoot = workspaceRoot;
        StateDir = stateDir;
    }

    public string ContentRoot { get; }

    public PiscinePaths Content => new(ContentRoot);

    public string WorkspaceRoot { get; }

    public string StateDir { get; }

    public string ProgressPath => Path.Combine(StateDir, "progress.json");

    public string WorkspaceExerciseDir(string moduleId, string exerciseId) =>
        Path.Combine(WorkspaceRoot, moduleId, exerciseId);

    /// <summary>Résout depuis les variables d'environnement, avec des valeurs par défaut.</summary>
    public static PiscineLayout FromEnvironment()
    {
        var content = Environment.GetEnvironmentVariable("PISCINE_CONTENT")
            ?? Path.Combine(AppContext.BaseDirectory, "content");

        var home = Environment.GetEnvironmentVariable("PISCINE_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "piscine");

        return new PiscineLayout(content, Path.Combine(home, "workspace"), Path.Combine(home, ".state"));
    }
}
```

- [ ] **Step 4 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Core.Tests --filter PiscineLayoutTests`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/PiscineLayout.cs tests/Piscine.Core.Tests/PiscineLayoutTests.cs
git commit -m "feat(core): PiscineLayout resout content/workspace/etat

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : `SubmissionLoader` (+ TempDir pour Grading.Tests)

**Files:**
- Create: `tests/Piscine.Grading.Tests/TempDir.cs`
- Create: `src/Piscine.Grading/SubmissionLoader.cs`
- Test: `tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs`

- [ ] **Step 1 : Copier l'utilitaire `TempDir` dans Grading.Tests**

`tests/Piscine.Grading.Tests/TempDir.cs` :
```csharp
using System;
using System.IO;

namespace Piscine.Grading.Tests;

/// <summary>Dossier temporaire jetable pour tests hermétiques.</summary>
public sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "piscine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string relativePath, string content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
```

- [ ] **Step 2 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs` :
```csharp
using System.IO;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class SubmissionLoaderTests
{
    [Fact]
    public void Load_ReadsDeliverablesFromWorkspaceAndGraderFilesFromContent()
    {
        using var dir = new TempDir();
        // Contenu de l'exercice (manifest + grader caché).
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: unit
                test_files: [grader/Tests.cs]
            """);
        dir.WriteFile(Path.Combine("content", "grader", "Tests.cs"), "// tests caches");
        // Code de la recrue dans le workspace.
        dir.WriteFile(Path.Combine("ws", "Hello.cs"), "// mon code");

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));

        Assert.Equal("ex00", submission.Manifest.Id);
        Assert.Equal("// mon code", submission.Context.Sources["Hello.cs"]);
        Assert.Equal("// tests caches", submission.Context.GraderFiles["grader/Tests.cs"]);
    }

    [Fact]
    public void Load_OmitsMissingDeliverables()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "manifest.yaml"), """
            id: ex00
            deliverables: [Absent.cs]
            """);
        Directory.CreateDirectory(dir.Combine("ws"));

        var submission = SubmissionLoader.Load(dir.Combine("content"), dir.Combine("ws"));

        Assert.Empty(submission.Context.Sources);
    }
}
```

- [ ] **Step 3 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter SubmissionLoaderTests`
Expected : FAIL — `SubmissionLoader` introuvable.

- [ ] **Step 4 : Implémenter `SubmissionLoader`**

`src/Piscine.Grading/SubmissionLoader.cs` :
```csharp
using System.Collections.Generic;
using System.IO;
using Piscine.Core.Content;

namespace Piscine.Grading;

/// <summary>Assemble une <see cref="ExerciseSubmission"/> depuis le disque.</summary>
public static class SubmissionLoader
{
    public static ExerciseSubmission Load(string exerciseContentDir, string workspaceExerciseDir)
    {
        var manifest = ExerciseManifestLoader.Load(exerciseContentDir);

        var sources = new Dictionary<string, string>();
        foreach (var deliverable in manifest.Deliverables)
        {
            var path = Path.Combine(workspaceExerciseDir, deliverable);
            if (File.Exists(path))
            {
                sources[deliverable] = File.ReadAllText(path);
            }
        }

        var graderFiles = new Dictionary<string, string>();
        foreach (var step in manifest.Grading)
        {
            foreach (var testFile in step.TestFiles)
            {
                var path = Path.Combine(exerciseContentDir, testFile);
                if (File.Exists(path))
                {
                    graderFiles[testFile] = File.ReadAllText(path);
                }
            }
        }

        return new ExerciseSubmission(manifest, new GradingContext(sources, graderFiles));
    }
}
```

- [ ] **Step 5 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter SubmissionLoaderTests`
Expected : PASS (2 tests).

- [ ] **Step 6 : Commit**

```bash
git add tests/Piscine.Grading.Tests/TempDir.cs src/Piscine.Grading/SubmissionLoader.cs tests/Piscine.Grading.Tests/SubmissionLoaderTests.cs
git commit -m "feat(grading): SubmissionLoader assemble depuis le disque

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : `ResultFormatter`

**Files:**
- Create: `src/Piscine.Grading/ResultFormatter.cs`
- Test: `tests/Piscine.Grading.Tests/ResultFormatterTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Grading.Tests/ResultFormatterTests.cs` :
```csharp
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ResultFormatterTests
{
    [Fact]
    public void Format_ARevoir_ShowsMessagesAndCourseRef()
    {
        var result = new ExerciseGradingResult("ex00", new[]
        {
            GraderResult.Failure("io", "La sortie ne correspond pas.")
        });
        var feedback = new FeedbackConfig { CourseRef = "cours.md#hello" };

        var text = ResultFormatter.Format(result, feedback);

        Assert.Contains("ex00", text);
        Assert.Contains("À revoir", text);
        Assert.Contains("La sortie ne correspond pas.", text);
        Assert.Contains("cours.md#hello", text);
    }

    [Fact]
    public void Format_Reussi_ShowsSuccess()
    {
        var result = new ExerciseGradingResult("ex00", new[] { GraderResult.Success("io") });

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Réussi", text);
    }

    [Fact]
    public void Format_NonCorrige_ShowsNotGraded()
    {
        var result = ExerciseGradingResult.NotGraded("ex02");

        var text = ResultFormatter.Format(result, new FeedbackConfig());

        Assert.Contains("Non corrigé", text);
    }
}
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ResultFormatterTests`
Expected : FAIL — `ResultFormatter` introuvable.

- [ ] **Step 3 : Implémenter `ResultFormatter`**

`src/Piscine.Grading/ResultFormatter.cs` :
```csharp
using System.Text;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Rend un résultat de correction en texte éducatif pour la console.</summary>
public static class ResultFormatter
{
    public static string Format(ExerciseGradingResult result, FeedbackConfig feedback)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {result.ExerciseId} : {Label(result.Status)} ===");

        foreach (var graderResult in result.Results)
        {
            foreach (var message in graderResult.Messages)
            {
                sb.AppendLine($"[{graderResult.GraderType}] {message}");
            }
        }

        if (result.Status == GraderStatus.ARevoir && !string.IsNullOrWhiteSpace(feedback.CourseRef))
        {
            sb.AppendLine($"→ Revois le cours : {feedback.CourseRef}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string Label(GraderStatus status) => status switch
    {
        GraderStatus.Reussi => "Réussi",
        GraderStatus.ARevoir => "À revoir",
        _ => "Non corrigé"
    };
}
```

- [ ] **Step 4 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter ResultFormatterTests`
Expected : PASS (3 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/ResultFormatter.cs tests/Piscine.Grading.Tests/ResultFormatterTests.cs
git commit -m "feat(grading): ResultFormatter rend un feedback educatif

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7 : `CheckCommand` (intégration)

**Files:**
- Create: `src/Piscine.Grading/CheckCommand.cs`
- Test: `tests/Piscine.Grading.Tests/CheckCommandTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue** (exercice correct → exit 0 + progression Reussi ; faux → exit 1 ; introuvable → exit 2)

`tests/Piscine.Grading.Tests/CheckCommandTests.cs` :
```csharp
using System.IO;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class CheckCommandTests
{
    private static PiscineLayout Setup(TempDir dir, string deliverableContent)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), "id: 00-setup\norder: 0\n");
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """);
        dir.WriteFile(Path.Combine("ws", "00-setup", "ex00", "Hello.cs"), deliverableContent);
        return new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));
    }

    [Fact]
    public void Run_Reussi_ReturnsZero_AndRecordsProgress()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("ex00");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
    }

    [Fact]
    public void Run_ARevoir_ReturnsOne()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"non\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("ex00");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("cours.md#hello", result.Output);
    }

    [Fact]
    public void Run_UnknownExercise_ReturnsTwo()
    {
        using var dir = new TempDir();
        var layout = Setup(dir, "System.Console.Write(\"ok\");");

        var result = new CheckCommand(layout, Graders.Default()).Run("inconnu");

        Assert.Equal(2, result.ExitCode);
    }
}
```

- [ ] **Step 2 : Lancer le test (échec attendu)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter CheckCommandTests`
Expected : FAIL — `CheckCommand` introuvable.

- [ ] **Step 3 : Implémenter `CheckCommand`**

`src/Piscine.Grading/CheckCommand.cs` :
```csharp
using System;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Progression;

namespace Piscine.Grading;

/// <summary>Résultat d'une commande : code de sortie + texte à afficher.</summary>
public sealed record CommandResult(int ExitCode, string Output);

/// <summary>Corrige un exercice localement (boucle <c>check</c>) et persiste la progression.</summary>
public sealed class CheckCommand
{
    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public CheckCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    public CommandResult Run(string exerciseId)
    {
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        if (location is null)
        {
            return new CommandResult(2, $"Exercice introuvable : {exerciseId}");
        }

        var workspaceDir = _layout.WorkspaceExerciseDir(location.ModuleId, exerciseId);
        var submission = SubmissionLoader.Load(location.ContentDir, workspaceDir);
        var result = _grader.Grade(submission.Manifest, submission.Context);

        var store = new ProgressStore(_layout.ProgressPath);
        var progress = store.Load();
        ProgressRecorder.Apply(progress, new[] { result }, DateTimeOffset.Now);
        store.Save(progress);

        var output = ResultFormatter.Format(result, submission.Manifest.Feedback);
        var exitCode = result.Status == GraderStatus.Reussi ? 0 : 1;
        return new CommandResult(exitCode, output);
    }
}
```

- [ ] **Step 4 : Lancer le test (succès)**

Run: `dotnet test tests/Piscine.Grading.Tests --filter CheckCommandTests`
Expected : PASS (3 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/CheckCommand.cs tests/Piscine.Grading.Tests/CheckCommandTests.cs
git commit -m "feat(grading): CheckCommand corrige en local et persiste la progression

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8 : Câblage CLI + vérification + push

**Files:**
- Modify: `src/Piscine.Cli/Piscine.Cli.csproj` (référence Grading)
- Modify: `src/Piscine.Cli/Program.cs`

- [ ] **Step 1 : Référencer `Piscine.Grading` depuis la CLI**

```bash
dotnet add src/Piscine.Cli reference src/Piscine.Grading
```
Expected : référence ajoutée.

- [ ] **Step 2 : Remplacer `src/Piscine.Cli/Program.cs`**

```csharp
using System.Reflection;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;
using Piscine.Grading;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var layout = PiscineLayout.FromEnvironment();

var command = args.Length > 0 ? args[0] : "status";

switch (command)
{
    case "list":
        ListModules(layout);
        return 0;

    case "start":
        return Start(layout, args);

    case "check":
        return Check(layout, args);

    case "status":
        Status(version, layout);
        return 0;

    default:
        Console.WriteLine($"Commande inconnue : {command}");
        Console.WriteLine("Commandes : list | start <exo> | check <exo> | status");
        return 64;
}

static void ListModules(PiscineLayout layout)
{
    var modules = ContentDiscovery.DiscoverModules(layout.Content);
    if (modules.Count == 0)
    {
        Console.WriteLine("Aucun module disponible pour le moment.");
        return;
    }

    foreach (var module in modules)
    {
        Console.WriteLine($"# {module.Id} — {module.Title}");
        foreach (var group in module.Groups)
        {
            Console.WriteLine($"  {group.Title}");
            foreach (var exercise in group.Exercises)
            {
                Console.WriteLine($"    - {exercise}");
            }
        }
    }
}

static int Start(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine start <exo>");
        return 64;
    }

    var exerciseId = args[1];
    var location = ContentLocator.FindExercise(layout.Content, exerciseId);
    if (location is null)
    {
        Console.WriteLine($"Exercice introuvable : {exerciseId}");
        return 2;
    }

    var workspaceDir = layout.WorkspaceExerciseDir(location.ModuleId, exerciseId);
    StarterInstaller.Install(location.ContentDir, workspaceDir);

    var subject = System.IO.Path.Combine(location.ContentDir, "subject.md");
    if (System.IO.File.Exists(subject))
    {
        Console.WriteLine(System.IO.File.ReadAllText(subject));
    }

    Console.WriteLine();
    Console.WriteLine($"Exercice prêt dans : {workspaceDir}");
    Console.WriteLine($"Quand tu as codé : piscine check {exerciseId}");
    return 0;
}

static int Check(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine check <exo>");
        return 64;
    }

    var result = new CheckCommand(layout, Graders.Default()).Run(args[1]);
    Console.WriteLine(result.Output);
    return result.ExitCode;
}

static void Status(string version, PiscineLayout layout)
{
    Console.WriteLine(WelcomeBanner.Render(version));
    Console.WriteLine();

    var modules = ContentDiscovery.DiscoverModules(layout.Content);
    if (modules.Count == 0)
    {
        Console.WriteLine("Aucun module installé. (Le contenu arrivera dans une prochaine itération.)");
        return;
    }

    Console.WriteLine($"{modules.Count} module(s) disponible(s). Tape 'piscine list' pour les voir.");
}
```

- [ ] **Step 3 : Vérifier la compilation et l'exécution (contenu vide → messages gracieux)**

Run: `dotnet run --project src/Piscine.Cli -- status`
Expected : bannière + « Aucun module installé… ».

Run: `dotnet run --project src/Piscine.Cli -- list`
Expected : « Aucun module disponible pour le moment. ».

Run: `dotnet run --project src/Piscine.Cli -- check ex00`
Expected : « Exercice introuvable : ex00 » (code 2).

- [ ] **Step 4 : Lancer toute la suite en Release**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : PASS — tous verts (Core ~19 + Grading ~33).

- [ ] **Step 5 : Commit + push + CI**

```bash
git add src/Piscine.Cli/Piscine.Cli.csproj src/Piscine.Cli/Program.cs
git commit -m "feat(cli): commandes list/start/check/status

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success**. Si échec : `gh run view --log-failed`, corriger, commit, push, re-watch.

---

## Self-Review (effectué)

**Couverture (It.4) :** fabrique `Graders.Default` (T1) ; localisation d'exercice (T2) ; install starter (T3) ; résolution des chemins (T4) ; assemblage disque (T5) ; feedback éducatif (T6) ; boucle `check` intégrée + persistance progression (T7) ; câblage CLI list/start/check/status (T8). ✓

**Reporté à l'It.5 (git) :** dépôt bare + hook `post-receive` + `grade-received` (orchestration de groupe au push via `GroupGrader` déjà prêt), MinGit, commande `init`.

**Placeholders :** aucun. **Cohérence des types :** `Graders.Default()→ExerciseGrader` ; `ContentLocator.FindExercise(PiscinePaths,string)→ExerciseLocation?` avec `.ModuleId/.ExerciseId/.ContentDir` ; `StarterInstaller.Install(string,string)` ; `PiscineLayout(string,string,string)` + `.Content/.WorkspaceRoot/.ProgressPath/.WorkspaceExerciseDir(string,string)` ; `SubmissionLoader.Load(string,string)→ExerciseSubmission` ; `ResultFormatter.Format(ExerciseGradingResult,FeedbackConfig)→string` ; `CheckCommand(PiscineLayout,ExerciseGrader).Run(string)→CommandResult(ExitCode,Output)`. Réutilise `ContentDiscovery`, `ProgressStore`, `ProgressRecorder`, `WelcomeBanner` existants.

**Note :** en dev (`dotnet run`), `PiscineLayout.FromEnvironment` cherche `content` sous `AppContext.BaseDirectory` (bin/...) → vide ; définir `PISCINE_CONTENT` pour pointer le `content/` du repo. En zip, `content/` est à côté du binaire → résolu. La résolution est couverte par tests via le constructeur explicite ; le câblage Program est vérifié par exécution (Step 3).
