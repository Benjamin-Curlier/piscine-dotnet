# Itération 0 — Bootstrap — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mettre en place le squelette complet du dépôt Piscine .NET (solution, 4 projets, tests, config, CI, docs) sur GitHub avec une CI verte.

**Architecture:** Solution .NET 10 à 4 projets (`Piscine.Cli`, `Piscine.Core`, `Piscine.Grading`, `Piscine.Git`) + tests xUnit. La logique testable (chemins de contenu, bannière) vit dans `Piscine.Core` ; `Piscine.Cli` ne fait qu'orchestrer/afficher. `Grading` et `Git` sont créés vides à cette itération (leur logique et leurs tests arrivent aux It. 2 et 3). Config partagée via `Directory.Build.props` + `global.json`. CI GitHub Actions build+test sur push/PR.

**Tech Stack:** .NET 10, C# (net10.0), xUnit (template SDK), GitHub Actions, gh CLI.

**Pré-requis vérifiés (machine du propriétaire) :** dotnet `10.0.204`, gh `2.89.0` authentifié (`Benjamin-Curlier`, scopes `repo`+`workflow`), git `2.53`. Le dépôt local est déjà initialisé et contient le commit de la spec (`3d82324`).

**Convention de commit :** chaque commit se termine par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. Toutes les commandes s'exécutent depuis `C:/Users/bencu/source/repos/piscine-dotnet`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `global.json` | Épingle la bande de SDK .NET utilisée |
| `Directory.Build.props` | Propriétés MSBuild communes (Nullable, ImplicitUsings, WarningsAsErrors) |
| `.editorconfig` | Style de code repo-wide (base de la future norme) |
| `.gitignore` | Ignore standard .NET (bin/obj/...) |
| `Piscine.sln` | Solution liant tous les projets |
| `src/Piscine.Core/PiscinePaths.cs` | Calcule les chemins `modules/` et `rushes/` sous un content root |
| `src/Piscine.Core/WelcomeBanner.cs` | Construit le texte de la bannière d'accueil (logique pure, testable) |
| `src/Piscine.Cli/Program.cs` | Point d'entrée : affiche la bannière |
| `src/Piscine.Grading/` | Projet vide (logique en It. 2) |
| `src/Piscine.Git/` | Projet vide (logique en It. 3) |
| `tests/Piscine.Core.Tests/` | Tests xUnit de Core |
| `content/`, `docs/` | Arborescence pédagogique + guide contributeur |
| `.github/workflows/ci.yml` | Pipeline build + test |
| `README.md` | Présentation + quickstart |

---

## Task 1 : Configuration repo + solution

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `.gitignore`
- Create: `.editorconfig`
- Create: `Piscine.sln`

- [ ] **Step 1 : Créer la solution et le .gitignore via templates SDK**

```bash
dotnet new sln --name Piscine
dotnet new gitignore
```
Expected : `Piscine.sln` et `.gitignore` créés.

- [ ] **Step 2 : Créer `global.json`** (épingle la bande SDK 10.0)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 3 : Créer `Directory.Build.props`** (propriétés communes — analyseurs au niveau par défaut pour éviter de casser le scaffolding)

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4 : Créer `.editorconfig`** (base de style, sera affinée pour la norme en It. 2)

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space

[*.cs]
indent_size = 4
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:warning
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
dotnet_style_require_accessibility_modifiers = always:warning

[*.{json,yml,yaml,csproj,props,targets}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

- [ ] **Step 5 : Vérifier que la solution restaure**

Run: `dotnet build Piscine.sln`
Expected : PASS — « Build succeeded » (solution vide, 0 projet).

- [ ] **Step 6 : Commit**

```bash
git add global.json Directory.Build.props .gitignore .editorconfig Piscine.sln
git commit -m "chore: configuration repo et solution

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : Projets Core + Core.Tests

**Files:**
- Create: `src/Piscine.Core/Piscine.Core.csproj` (+ supprime `Class1.cs`)
- Create: `tests/Piscine.Core.Tests/Piscine.Core.Tests.csproj`

- [ ] **Step 1 : Créer les deux projets et les câbler**

```bash
dotnet new classlib --output src/Piscine.Core
dotnet new xunit --output tests/Piscine.Core.Tests
rm src/Piscine.Core/Class1.cs
dotnet add tests/Piscine.Core.Tests reference src/Piscine.Core
dotnet sln add src/Piscine.Core tests/Piscine.Core.Tests
```
Expected : projets créés, référence ajoutée, ajoutés à la solution.

- [ ] **Step 2 : Vérifier build + test (le template xUnit a 0 test, doit passer)**

Run: `dotnet test Piscine.sln`
Expected : PASS — build OK, « Passed! » ou « No test ».

- [ ] **Step 3 : Commit**

```bash
git add src/Piscine.Core tests/Piscine.Core.Tests Piscine.sln
git commit -m "feat: squelette Piscine.Core et ses tests

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : `PiscinePaths` (TDD)

**Files:**
- Test: `tests/Piscine.Core.Tests/PiscinePathsTests.cs`
- Create: `src/Piscine.Core/PiscinePaths.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/PiscinePathsTests.cs` :
```csharp
using System.IO;
using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class PiscinePathsTests
{
    [Fact]
    public void ModulesDirectory_IsModulesUnderRoot()
    {
        var paths = new PiscinePaths("/content");

        Assert.Equal(Path.Combine("/content", "modules"), paths.ModulesDirectory);
    }

    [Fact]
    public void RushesDirectory_IsRushesUnderRoot()
    {
        var paths = new PiscinePaths("/content");

        Assert.Equal(Path.Combine("/content", "rushes"), paths.RushesDirectory);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests`
Expected : FAIL — « The type or namespace name 'PiscinePaths' could not be found ».

- [ ] **Step 3 : Implémenter le minimum**

`src/Piscine.Core/PiscinePaths.cs` :
```csharp
using System.IO;

namespace Piscine.Core;

/// <summary>
/// Localise les dossiers de contenu pédagogique sous une racine donnée.
/// </summary>
public sealed class PiscinePaths
{
    public PiscinePaths(string contentRoot)
    {
        ContentRoot = contentRoot;
    }

    public string ContentRoot { get; }

    public string ModulesDirectory => Path.Combine(ContentRoot, "modules");

    public string RushesDirectory => Path.Combine(ContentRoot, "rushes");
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests`
Expected : PASS — « Passed! ».

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/PiscinePaths.cs tests/Piscine.Core.Tests/PiscinePathsTests.cs
git commit -m "feat(core): PiscinePaths localise modules et rushes

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `WelcomeBanner` (TDD)

**Files:**
- Test: `tests/Piscine.Core.Tests/WelcomeBannerTests.cs`
- Create: `src/Piscine.Core/WelcomeBanner.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/WelcomeBannerTests.cs` :
```csharp
using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class WelcomeBannerTests
{
    [Fact]
    public void Render_ContainsTitle()
    {
        var banner = WelcomeBanner.Render("1.2.3");

        Assert.Contains("Piscine .NET", banner);
    }

    [Fact]
    public void Render_ContainsVersion()
    {
        var banner = WelcomeBanner.Render("1.2.3");

        Assert.Contains("1.2.3", banner);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests`
Expected : FAIL — « 'WelcomeBanner' could not be found ».

- [ ] **Step 3 : Implémenter le minimum**

`src/Piscine.Core/WelcomeBanner.cs` :
```csharp
namespace Piscine.Core;

/// <summary>
/// Construit le texte de la bannière d'accueil affichée par la CLI.
/// </summary>
public static class WelcomeBanner
{
    public static string Render(string version)
    {
        return $"""
            ┌──────────────────────────────┐
            │        Piscine .NET          │
            │      bootcamp C# / git       │
            └──────────────────────────────┘
            version {version}
            """;
    }
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests`
Expected : PASS — « Passed! » (4 tests au total).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/WelcomeBanner.cs tests/Piscine.Core.Tests/WelcomeBannerTests.cs
git commit -m "feat(core): WelcomeBanner rend la banniere d'accueil

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : Projet CLI (point d'entrée)

**Files:**
- Create: `src/Piscine.Cli/Piscine.Cli.csproj`
- Modify: `src/Piscine.Cli/Program.cs`

- [ ] **Step 1 : Créer le projet console et le câbler**

```bash
dotnet new console --output src/Piscine.Cli
dotnet add src/Piscine.Cli reference src/Piscine.Core
dotnet sln add src/Piscine.Cli
```
Expected : projet console créé, référence Core ajoutée, ajouté à la solution.

- [ ] **Step 2 : Remplacer `src/Piscine.Cli/Program.cs`**

```csharp
using System.Reflection;
using Piscine.Core;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

Console.WriteLine(WelcomeBanner.Render(version));
Console.WriteLine();
Console.WriteLine("Itération 0 — squelette en place. Les commandes arrivent aux prochaines itérations.");
```

- [ ] **Step 3 : Lancer l'app pour vérifier la sortie**

Run: `dotnet run --project src/Piscine.Cli`
Expected : affiche la bannière « Piscine .NET » + la ligne « Itération 0 — squelette en place. ».

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Cli Piscine.sln
git commit -m "feat(cli): point d'entree affichant la banniere

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : Projets Grading et Git (squelettes)

**Files:**
- Create: `src/Piscine.Grading/Piscine.Grading.csproj` (+ supprime `Class1.cs`)
- Create: `src/Piscine.Git/Piscine.Git.csproj` (+ supprime `Class1.cs`)

> Ces projets restent vides à l'It. 0. Leur logique et leurs projets de tests sont créés aux It. 2 (Grading) et It. 3 (Git).

- [ ] **Step 1 : Créer les deux bibliothèques et les câbler**

```bash
dotnet new classlib --output src/Piscine.Grading
dotnet new classlib --output src/Piscine.Git
rm src/Piscine.Grading/Class1.cs src/Piscine.Git/Class1.cs
dotnet add src/Piscine.Grading reference src/Piscine.Core
dotnet add src/Piscine.Git reference src/Piscine.Core
dotnet sln add src/Piscine.Grading src/Piscine.Git
```

- [ ] **Step 2 : Ajouter un marqueur d'assembly dans chaque** (évite un projet sans aucun type)

`src/Piscine.Grading/AssemblyMarker.cs` :
```csharp
namespace Piscine.Grading;

/// <summary>Marqueur d'assembly. La logique de notation arrive à l'It. 2.</summary>
public static class AssemblyMarker;
```

`src/Piscine.Git/AssemblyMarker.cs` :
```csharp
namespace Piscine.Git;

/// <summary>Marqueur d'assembly. La logique git arrive à l'It. 3.</summary>
public static class AssemblyMarker;
```

- [ ] **Step 3 : Vérifier build complet**

Run: `dotnet build Piscine.sln`
Expected : PASS — tous les projets compilent.

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Grading src/Piscine.Git Piscine.sln
git commit -m "feat: squelettes Piscine.Grading et Piscine.Git

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7 : Arborescence contenu + docs contributeur

**Files:**
- Create: `content/modules/.gitkeep`
- Create: `content/rushes/.gitkeep`
- Create: `content/README.md`
- Create: `docs/contributing/ajouter-un-exercice.md`

- [ ] **Step 1 : Créer les dossiers de contenu** (placeholders pour versionner les dossiers vides)

`content/modules/.gitkeep` : (fichier vide)
`content/rushes/.gitkeep` : (fichier vide)

- [ ] **Step 2 : Créer `content/README.md`**

```markdown
# Contenu pédagogique

- `modules/<NN-slug>/` : un module = un dossier ordonné par `order` dans `module.yaml`.
- `rushes/<slug>/` : projets de synthèse solo.

Chaque module contient `module.yaml`, `cours.md`, et `exercises/<id>/`.
Chaque exercice contient `manifest.yaml`, `subject.md`, `starter/`, `grader/`, `solution/`.

Voir `docs/contributing/ajouter-un-exercice.md`. Les dossiers `solution/` ne sont jamais
inclus dans le zip distribué.
```

- [ ] **Step 3 : Créer `docs/contributing/ajouter-un-exercice.md`**

```markdown
# Ajouter un exercice

1. Générer le squelette : `piscine new exercise <module> <id>` *(commande disponible à l'It. 1+)*.
   En attendant, copier un exercice existant.
2. Renseigner `manifest.yaml` (deliverables, grading, feedback) et `subject.md` (énoncé).
3. Placer les fichiers fournis dans `starter/`, les tests cachés dans `grader/`,
   et le corrigé de référence dans `solution/`.
4. Ajouter l'`id` de l'exercice dans un groupe de `module.yaml` (l'ordre = correction séquentielle).
5. Valider : `piscine validate-content` *(disponible à l'It. 1+)* — vérifie que le corrigé
   passe ses propres graders. La CI exécute la même vérification.

Aucune recompilation de l'application n'est nécessaire : le contenu est découvert au démarrage.
```

- [ ] **Step 4 : Commit**

```bash
git add content docs/contributing
git commit -m "docs: arborescence de contenu et guide contributeur

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8 : README

**Files:**
- Create: `README.md`

- [ ] **Step 1 : Créer `README.md`**

```markdown
# Piscine .NET

Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux **C#** (.NET 10),
avec moulinette auto-correctrice locale, apprentissage du vrai **git**, et distribution autonome.

## Pour la recrue

Télécharge le zip de la [dernière release](../../releases/latest), dézippe, puis lance `piscine`
(`piscine.exe` sous Windows). Aucun SDK à installer.

## Pour développer le bootcamp

Pré-requis : SDK .NET 10.

```bash
dotnet build Piscine.sln
dotnet test Piscine.sln
dotnet run --project src/Piscine.Cli
```

## Structure

- `src/` : application (`Piscine.Cli`) et bibliothèques (`Core`, `Grading`, `Git`).
- `tests/` : tests xUnit.
- `content/` : cours, exercices et rushes (voir `content/README.md`).
- `docs/` : specs (`docs/superpowers/specs/`), plans (`docs/superpowers/plans/`),
  guide contributeur (`docs/contributing/`).

Design complet : `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md`.
```

- [ ] **Step 2 : Commit**

```bash
git add README.md
git commit -m "docs: README du projet

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 9 : Pipeline CI (build + test)

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1 : Créer `.github/workflows/ci.yml`**

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore Piscine.sln

      - name: Build
        run: dotnet build Piscine.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test Piscine.sln --configuration Release --no-build --verbosity normal
```

- [ ] **Step 2 : Vérifier localement que la commande CI passe** (en Release, comme la CI)

Run: `dotnet test Piscine.sln --configuration Release`
Expected : PASS — build Release OK, 4 tests passants.

- [ ] **Step 3 : Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: pipeline build et test GitHub Actions

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 10 : Créer le dépôt GitHub et vérifier la CI

> gh est déjà authentifié (`Benjamin-Curlier`, scopes `repo`+`workflow`). **Avant le Step 1, confirmer avec le propriétaire le nom et la visibilité du dépôt** (défaut proposé : `piscine-dotnet`, **privé**).

- [ ] **Step 1 : Créer le dépôt distant et pousser** (défaut privé)

```bash
gh repo create piscine-dotnet --private --source . --remote origin --push
```
Expected : dépôt créé sous `Benjamin-Curlier/piscine-dotnet`, branche `main` poussée.

- [ ] **Step 2 : Vérifier que la CI se déclenche et passe au vert**

```bash
gh run watch --exit-status
```
Expected : le run « CI » se termine en **success**.

- [ ] **Step 3 : Si la CI échoue**, lire les logs et corriger

```bash
gh run view --log-failed
```
Corriger localement, commit, push, puis relancer `gh run watch --exit-status` jusqu'au vert.

---

## Self-Review (effectué)

**Couverture spec (It. 0) :** solution + 4 projets ✓ (T2, T5, T6) · tests xUnit ✓ (T2-4) · `.editorconfig` ✓ (T1) · arborescence `content/`+`docs/` ✓ (T7) · CI build+test ✓ (T9) · README + guide contributeur ✓ (T7-8) · repo GitHub via gh ✓ (T10).

**Hors périmètre It. 0 (volontaire) :** `release.yml`, graders Roslyn, intégration git/LibGit2Sharp, parsing YAML — planifiés aux It. 1-5. `Grading.Tests`/`Git.Tests` créés à leurs itérations respectives.

**Cohérence des types :** `PiscinePaths(string)` / `.ModulesDirectory` / `.RushesDirectory` et `WelcomeBanner.Render(string)` utilisés de façon identique entre tests, implémentation et `Program.cs`.

**Placeholders :** aucun TODO/TBD ; tout le code et toutes les commandes sont complets.
