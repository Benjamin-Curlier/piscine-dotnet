# Itération 6a — `validate-content` (garde-fou qualité du contenu) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Cases `- [ ]` pour le suivi.

**Goal:** Garantir qu'un exercice livré est cohérent : manifest valide, fichiers de graders référencés présents, **et surtout que le corrigé `solution/` passe bien ses propres graders**. Exposer `piscine validate-content` et l'ajouter en **gate CI** (spec §4.5 / §7). Sur le contenu actuel (vide), la commande passe trivialement ; elle devient un vrai filet dès le Module 00 (It.8).

**Architecture:** `ContentValidator` dans `Piscine.Grading` (a besoin du moteur + de la découverte de contenu), réutilise `ContentDiscovery`, `ContentLocator`, `ExerciseManifestLoader`, `SubmissionLoader`, `Graders.Default`. Le corrigé est noté comme une soumission ordinaire : `SubmissionLoader.Load(contentDir, contentDir/solution)` (les fichiers `solution/<livrable>` jouent le rôle du workspace). `Piscine.Cli` route la commande ; `ci.yml` ajoute un step.

**Tech Stack:** .NET 10, xUnit. Aucune nouvelle dépendance. (Décisions It.6 : packaging = **self-contained dossier** non single-file → traité en It.6b ; cette It.6a ne touche pas au packaging.)

**Contexte repo (It.0→It.5 faites, 57 tests, CI verte) :** `Graders.Default()→ExerciseGrader` ; `SubmissionLoader.Load(contentDir,wsDir)→ExerciseSubmission{Manifest,Context}` (lit `manifest.Deliverables` depuis wsDir, `TestFiles` depuis contentDir) ; `ContentDiscovery.DiscoverModules(PiscinePaths)→IReadOnlyList<Module>` ; `ContentLocator.FindExercise(PiscinePaths,string)→ExerciseLocation?{ModuleId,ExerciseId,ContentDir}` ; `ExerciseManifestLoader.Load(dir)→ExerciseManifest{Id,Deliverables,Grading[].TestFiles,...}` ; `ExerciseGrader.Grade(manifest,context)→ExerciseGradingResult{Status,Results[].Messages}` ; `GraderStatus.Reussi`. YAML : `IgnoreUnmatchedProperties` (les clés `solution:`/`constraints:` des manifests réels sont ignorées sans erreur). `tests/Piscine.Grading.Tests` a `TempDir` + parallélisation désactivée. CLI `Program.cs` route déjà `list/start/check/status/init/grade-received`. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Grading/ContentValidator.cs` | `ContentIssue`, `ContentValidationReport`, `ContentValidator` |
| `src/Piscine.Cli/Program.cs` | (modifié) commande `validate-content` |
| `.github/workflows/ci.yml` | (modifié) step gate `validate-content` |
| `tests/Piscine.Grading.Tests/ContentValidatorTests.cs` | tests |

---

## Task 1 : `ContentValidator`

**Files:**
- Create: `src/Piscine.Grading/ContentValidator.cs`
- Test: `tests/Piscine.Grading.Tests/ContentValidatorTests.cs`

- [ ] **Step 1 : Écrire les tests qui échouent**

`tests/Piscine.Grading.Tests/ContentValidatorTests.cs` :
```csharp
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ContentValidatorTests
{
    private static PiscineLayout LayoutFor(TempDir dir) =>
        new(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

    private static void WriteExercise(TempDir dir, string manifestYaml, string? solutionHello)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), manifestYaml);
        if (solutionHello is not null)
        {
            dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "solution", "Hello.cs"), solutionHello);
        }
    }

    private const string IoManifest = """
        id: ex00
        deliverables: [Hello.cs]
        grading:
          - type: io
            cases:
              - expect_stdout: "ok"
                expect_exit: 0
        """;

    [Fact]
    public void Validate_EmptyContent_IsValid()
    {
        using var dir = new TempDir();
        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_SolutionPassesItsGraders_IsValid()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.True(report.IsValid);
    }

    [Fact]
    public void Validate_SolutionFails_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, "System.Console.Write(\"non\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.ExerciseId == "ex00" && i.Message.Contains("corrigé"));
    }

    [Fact]
    public void Validate_MissingSolutionDir_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, IoManifest, solutionHello: null);

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.Message.Contains("solution/"));
    }

    [Fact]
    public void Validate_MissingGraderFile_ReportsIssue()
    {
        using var dir = new TempDir();
        WriteExercise(dir, """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: unit
                test_files: [grader/Tests.cs]
            """, "System.Console.Write(\"ok\");");

        var report = new ContentValidator(Graders.Default()).Validate(LayoutFor(dir));

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, i => i.Message.Contains("grader/Tests.cs"));
    }
}
```

- [ ] **Step 2 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Grading.Tests --filter ContentValidatorTests` → FAIL (`ContentValidator` introuvable).

- [ ] **Step 3 : Implémenter `ContentValidator`**

`src/Piscine.Grading/ContentValidator.cs` :
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>Un problème détecté dans le contenu pédagogique.</summary>
public sealed record ContentIssue(string ExerciseId, string Message);

/// <summary>Rapport de validation du contenu.</summary>
public sealed class ContentValidationReport
{
    public ContentValidationReport(IReadOnlyList<ContentIssue> issues) => Issues = issues;

    public IReadOnlyList<ContentIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;
}

/// <summary>
/// Garde-fou qualité : vérifie que chaque exercice référencé a un manifest valide,
/// des fichiers de graders présents, et un corrigé <c>solution/</c> qui passe ses
/// propres graders. (spec §4.5)
/// </summary>
public sealed class ContentValidator
{
    public const string SolutionDirName = "solution";

    private readonly ExerciseGrader _grader;

    public ContentValidator(ExerciseGrader grader) => _grader = grader;

    public ContentValidationReport Validate(PiscineLayout layout)
    {
        var issues = new List<ContentIssue>();
        foreach (var module in ContentDiscovery.DiscoverModules(layout.Content))
        {
            foreach (var exerciseId in module.Groups.SelectMany(g => g.Exercises))
            {
                ValidateExercise(layout, exerciseId, issues);
            }
        }

        return new ContentValidationReport(issues);
    }

    private void ValidateExercise(PiscineLayout layout, string exerciseId, List<ContentIssue> issues)
    {
        var location = ContentLocator.FindExercise(layout.Content, exerciseId);
        if (location is null)
        {
            issues.Add(new ContentIssue(exerciseId, "référencé dans un groupe mais introuvable dans le contenu."));
            return;
        }

        ExerciseManifest manifest;
        try
        {
            manifest = ExerciseManifestLoader.Load(location.ContentDir);
        }
        catch (Exception e)
        {
            issues.Add(new ContentIssue(exerciseId, $"manifest.yaml invalide : {e.Message}"));
            return;
        }

        foreach (var testFile in manifest.Grading.SelectMany(s => s.TestFiles))
        {
            if (!File.Exists(Path.Combine(location.ContentDir, testFile)))
            {
                issues.Add(new ContentIssue(exerciseId, $"fichier de grader manquant : {testFile}"));
            }
        }

        var solutionDir = Path.Combine(location.ContentDir, SolutionDirName);
        if (!Directory.Exists(solutionDir))
        {
            issues.Add(new ContentIssue(exerciseId, "dossier solution/ manquant (corrigé de référence requis)."));
            return;
        }

        var submission = SubmissionLoader.Load(location.ContentDir, solutionDir);
        foreach (var deliverable in manifest.Deliverables)
        {
            if (!submission.Context.Sources.ContainsKey(deliverable))
            {
                issues.Add(new ContentIssue(exerciseId, $"corrigé manquant pour le livrable : {deliverable}"));
            }
        }

        var result = _grader.Grade(submission.Manifest, submission.Context);
        if (result.Status != GraderStatus.Reussi)
        {
            var detail = string.Join(" ; ", result.Results.SelectMany(r => r.Messages));
            issues.Add(new ContentIssue(exerciseId, $"le corrigé ne passe pas ses graders : {detail}"));
        }
    }
}
```

- [ ] **Step 4 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Grading.Tests --filter ContentValidatorTests` → PASS (5 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/ContentValidator.cs tests/Piscine.Grading.Tests/ContentValidatorTests.cs
git commit -m "feat(grading): ContentValidator verifie manifests + corriges solution

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : Commande CLI `validate-content` + gate CI

**Files:**
- Modify: `src/Piscine.Cli/Program.cs`
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1 : Ajouter la commande dans `Program.cs`**

Dans le `switch`, avant `default`, ajouter :
```csharp
    case "validate-content":
        return ValidateContent(layout);
```
Mettre à jour la ligne d'usage du `default` : ajouter `| validate-content`.

Ajouter la fonction locale :
```csharp
static int ValidateContent(PiscineLayout layout)
{
    var report = new ContentValidator(Graders.Default()).Validate(layout);
    if (report.IsValid)
    {
        Console.WriteLine("Contenu valide.");
        return 0;
    }

    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"[{issue.ExerciseId}] {issue.Message}");
    }

    Console.WriteLine($"{report.Issues.Count} problème(s) de contenu.");
    return 1;
}
```

- [ ] **Step 2 : Vérifier l'exécution sur le contenu du repo (vide → valide)**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content`
Expected : « Contenu valide. » (code 0).

- [ ] **Step 3 : Ajouter le gate dans `ci.yml`**

Après le step `Test`, ajouter :
```yaml
      - name: Validate content
        run: dotnet run --project src/Piscine.Cli --configuration Release --no-restore -- validate-content
        env:
          PISCINE_CONTENT: ${{ github.workspace }}/content
```

- [ ] **Step 4 : Suite complète en Release**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : tous verts (total attendu ≈ 62 : +5 ContentValidator).

- [ ] **Step 5 : Commit + push + CI**

```bash
git add src/Piscine.Cli/Program.cs .github/workflows/ci.yml
git commit -m "feat(cli): commande validate-content + gate CI

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success** (build + test + validate-content sur contenu vide).

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.6a) :** `ContentValidator` (corrigé passe ses graders, corrigé KO, solution/ manquant, fichier grader manquant, contenu vide) (T1) ; commande CLI `validate-content` + gate CI (T2). ✓

**Réutilise :** `Graders.Default`, `SubmissionLoader`, `ContentDiscovery`, `ContentLocator`, `ExerciseManifestLoader`. Le corrigé est noté via la convention `solution/<livrable>` (les `solution/` ne sont jamais zippés — exclus du paquet en It.6b).

**Reporté à It.6b (packaging) :** `release.yml` (publish self-contained **dossier** par OS : win-x64/linux-x64/osx-arm64, zip = binaire + `content/` sans `solution/` + MinGit Windows + script de lancement, attaché à la Release), exclusion `solution/` du paquet, et **`docs/mise-en-oeuvre.md`**. Décision actée : pas de single-file (évite les écueils TPA + `Assembly.Location` du grader).

**Cohérence des types :** `ContentValidator(ExerciseGrader).Validate(PiscineLayout)→ContentValidationReport{IsValid,Issues}` ; `ContentIssue(string ExerciseId,string Message)`.
