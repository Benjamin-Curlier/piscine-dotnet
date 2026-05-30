# Itération 6b — Packaging & mise en œuvre — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Cases `- [ ]` pour le suivi.

**Goal:** Produire l'artefact distribuable : sur un tag `v*`, la CI publie un **dossier self-contained par OS** (win-x64 / linux-x64 / osx-arm64), assemble un zip = binaire `piscine` + `content/` (**sans `solution/`**) + MinGit (Windows) + lanceur, et l'attache à la GitHub Release. Plus la **doc de mise en œuvre** (`docs/mise-en-oeuvre.md`).

**Architecture:** Le choix « dossier self-contained » (décidé à l'It.6a) est **vérifié** : le binaire publié compile via Roslyn (`io`) et exécute les tests xUnit (`unit`) depuis le dossier (refs résolues sur les vraies DLL ; `Assembly.Location` non vide). `content/` placé à côté du binaire → `PiscineLayout.FromEnvironment` le résout via `AppContext.BaseDirectory/content` (zéro config). L'exclusion des corrigés est faite par un composant **testable** `ContentPackager` (Core) + commande CLI `package-content`, appelé par `release.yml`. Le hook écrit déjà `Environment.ProcessPath` (= vrai `piscine`/`piscine.exe` dans le dossier) → rien à changer côté hook.

**Tech Stack:** .NET 10, xUnit, GitHub Actions, `gh release`. MinGit téléchargé depuis git-for-windows. Aucune nouvelle dépendance NuGet.

**Vérifié localement avant ce plan :** `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=false` → dossier 228 fichiers/105 Mo contenant `Microsoft.CodeAnalysis.CSharp.dll`, `LibGit2Sharp.dll`, `xunit.*.dll`. Le binaire publié `validate-content` rend « Contenu valide. » sur un contenu io **et** unit. Binaire actuel nommé `Piscine.Cli` → à renommer `piscine`.

**Contexte repo (It.0→It.6a, 62 tests, CI verte) :** `PiscineLayout.FromEnvironment()` (content = `PISCINE_CONTENT` ?? `AppContext.BaseDirectory/content`) ; `ContentValidator`, `validate-content` en gate CI ; `Piscine.Cli/Program.cs` route `list/start/check/status/init/grade-received/validate-content`. `tests/Piscine.Core.Tests` a `TempDir`. `.gitignore` ignore `artifacts/`. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. **Rappel process : NE PAS chaîner `git commit` et `git push` dans un même appel** (un refus du push annule le commit) — appels séparés.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Cli/Piscine.Cli.csproj` | (modifié) `<AssemblyName>piscine</AssemblyName>` |
| `src/Piscine.Core/Content/ContentPackager.cs` | Copie le contenu en excluant tout dossier `solution/` |
| `src/Piscine.Cli/Program.cs` | (modifié) commande `package-content <src> <dest>` |
| `build/launchers/start-piscine.cmd` | Lanceur Windows (MinGit sur PATH + shell) |
| `.github/workflows/release.yml` | Publish par OS + zip + GitHub Release (sur tag) |
| `docs/mise-en-oeuvre.md` | Préparation poste recrue (spec §10.1) |
| `tests/Piscine.Core.Tests/ContentPackagerTests.cs` | tests |

---

## Task 1 : Renommer le binaire + `ContentPackager`

**Files:**
- Modify: `src/Piscine.Cli/Piscine.Cli.csproj`
- Create: `src/Piscine.Core/Content/ContentPackager.cs`
- Test: `tests/Piscine.Core.Tests/ContentPackagerTests.cs`

- [ ] **Step 1 : Renommer le binaire**

Dans `src/Piscine.Cli/Piscine.Cli.csproj`, ajouter dans le `<PropertyGroup>` :
```xml
    <AssemblyName>piscine</AssemblyName>
```

- [ ] **Step 2 : Écrire les tests qui échouent**

`tests/Piscine.Core.Tests/ContentPackagerTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentPackagerTests
{
    [Fact]
    public void CopyWithoutSolutions_CopiesContent_ButOmitsSolutionDirs()
    {
        using var dir = new TempDir();
        var exo = Path.Combine("src", "modules", "00", "exercises", "ex00");
        dir.WriteFile(Path.Combine(exo, "manifest.yaml"), "id: ex00");
        dir.WriteFile(Path.Combine(exo, "subject.md"), "énoncé");
        dir.WriteFile(Path.Combine(exo, "starter", "README.md"), "départ");
        dir.WriteFile(Path.Combine(exo, "solution", "Hello.cs"), "// corrigé secret");

        ContentPackager.CopyWithoutSolutions(dir.Combine("src"), dir.Combine("out"));

        var outExo = Path.Combine(dir.Combine("out"), "modules", "00", "exercises", "ex00");
        Assert.True(File.Exists(Path.Combine(outExo, "manifest.yaml")));
        Assert.True(File.Exists(Path.Combine(outExo, "subject.md")));
        Assert.True(File.Exists(Path.Combine(outExo, "starter", "README.md")));
        Assert.False(Directory.Exists(Path.Combine(outExo, "solution")));
    }

    [Fact]
    public void CopyWithoutSolutions_OmitsFileNamedLikeButNestedUnderSolution_Only()
    {
        using var dir = new TempDir();
        // Un fichier "solution.md" (pas un dossier solution/) doit être conservé.
        dir.WriteFile(Path.Combine("src", "solution.md"), "doc");
        dir.WriteFile(Path.Combine("src", "ex", "solution", "S.cs"), "secret");

        ContentPackager.CopyWithoutSolutions(dir.Combine("src"), dir.Combine("out"));

        Assert.True(File.Exists(Path.Combine(dir.Combine("out"), "solution.md")));
        Assert.False(Directory.Exists(Path.Combine(dir.Combine("out"), "ex", "solution")));
    }
}
```

- [ ] **Step 3 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Core.Tests --filter ContentPackagerTests` → FAIL.

- [ ] **Step 4 : Implémenter `ContentPackager`**

`src/Piscine.Core/Content/ContentPackager.cs` :
```csharp
using System;
using System.IO;

namespace Piscine.Core.Content;

/// <summary>
/// Copie une arborescence de contenu vers une destination en EXCLUANT tout dossier
/// <c>solution/</c> (les corrigés de référence ne sont jamais distribués). (spec §3.3)
/// </summary>
public static class ContentPackager
{
    public const string SolutionDirName = "solution";

    public static void CopyWithoutSolutions(string sourceContentDir, string destContentDir)
    {
        foreach (var file in Directory.EnumerateFiles(sourceContentDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceContentDir, file);
            if (HasSolutionSegment(relative))
            {
                continue;
            }

            var destination = Path.Combine(destContentDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static bool HasSolutionSegment(string relativePath)
    {
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment.Equals(SolutionDirName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
```

- [ ] **Step 5 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Core.Tests --filter ContentPackagerTests` → PASS (2 tests).

- [ ] **Step 6 : Commit** (appel séparé du push)

```bash
git add src/Piscine.Cli/Piscine.Cli.csproj src/Piscine.Core/Content/ContentPackager.cs tests/Piscine.Core.Tests/ContentPackagerTests.cs
git commit -m "feat(core): ContentPackager exclut solution/ + binaire renomme piscine

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : Commande CLI `package-content`

**Files:**
- Modify: `src/Piscine.Cli/Program.cs`

- [ ] **Step 1 : Ajouter la commande**

Dans le `switch`, avant `default` :
```csharp
    case "package-content":
        return PackageContent(args);
```
Mettre à jour la ligne d'usage du `default` : ajouter `| package-content <src> <dest>`.

Ajouter la fonction locale (utilise `Piscine.Core.Content`) :
```csharp
static int PackageContent(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage : piscine package-content <dossier-source> <dossier-destination>");
        return 64;
    }

    ContentPackager.CopyWithoutSolutions(args[1], args[2]);
    Console.WriteLine($"Contenu empaqueté (sans solution/) → {args[2]}");
    return 0;
}
```
S'assurer que `using Piscine.Core.Content;` est présent en tête (déjà le cas).

- [ ] **Step 2 : Vérifier**

Run: `dotnet run --project src/Piscine.Cli -- package-content content artifacts/content-test`
Expected : « Contenu empaqueté … » (code 0), `artifacts/content-test/` contient `modules/`, `rushes/`, `README.md` (contenu actuel sans solution/). Puis `Remove-Item -Recurse artifacts`.

- [ ] **Step 3 : Suite complète en Release**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : tous verts (≈ 64 : +2 ContentPackager).

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Cli/Program.cs
git commit -m "feat(cli): commande package-content (assemble le contenu distribuable)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : `release.yml` + lanceur Windows

**Files:**
- Create: `build/launchers/start-piscine.cmd`
- Create: `.github/workflows/release.yml`

- [ ] **Step 1 : Lanceur Windows**

`build/launchers/start-piscine.cmd` (CRLF accepté ; ouvre un shell avec MinGit + piscine sur PATH) :
```bat
@echo off
rem Lanceur Piscine (Windows) : place le git portable (MinGit) et piscine sur le PATH,
rem puis ouvre une invite prête à l'emploi.
set "PISCINE_DIR=%~dp0"
set "PATH=%PISCINE_DIR%mingit\cmd;%PISCINE_DIR%;%PATH%"
echo Piscine prete. Exemples : piscine init   puis   piscine start ^<exo^>
cmd /k "cd /d %PISCINE_DIR%"
```

- [ ] **Step 2 : Écrire `release.yml`**

`.github/workflows/release.yml` :
```yaml
name: Release

on:
  push:
    tags: ['v*']

permissions:
  contents: write

env:
  MINGIT_VERSION: 2.47.1
  MINGIT_TAG: v2.47.1.windows.1

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Assembler le contenu (sans solution/)
        run: dotnet run --project src/Piscine.Cli --configuration Release -- package-content content artifacts/content

      - name: Publier, empaqueter, zipper par OS
        run: |
          set -euo pipefail
          mkdir -p dist artifacts/pkg
          curl -sL "https://github.com/git-for-windows/git/releases/download/${MINGIT_TAG}/MinGit-${MINGIT_VERSION}-64-bit.zip" -o mingit.zip
          for rid in linux-x64 win-x64 osx-arm64; do
            out="artifacts/pkg/piscine-$rid"
            dotnet publish src/Piscine.Cli --configuration Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$out"
            cp -r artifacts/content "$out/content"
            if [ "$rid" = "win-x64" ]; then
              mkdir -p "$out/mingit"
              unzip -q mingit.zip -d "$out/mingit"
              cp build/launchers/start-piscine.cmd "$out/start-piscine.cmd"
            fi
            ( cd artifacts/pkg && zip -qr "$GITHUB_WORKSPACE/dist/piscine-${{ github.ref_name }}-$rid.zip" "piscine-$rid" )
          done
          ls -lh dist

      - name: Créer la GitHub Release
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          gh release create "${{ github.ref_name }}" dist/*.zip \
            --title "Piscine .NET ${{ github.ref_name }}" \
            --notes "Binaires self-contained par OS (win-x64, linux-x64, osx-arm64). Dézippe, lis docs/mise-en-oeuvre.md."
```

- [ ] **Step 3 : Dry-run local de l'assemblage (1 RID, sans MinGit ni release)**

Vérifier la logique publish+content+zip pour win-x64 :
```bash
dotnet run --project src/Piscine.Cli -c Release -- package-content content artifacts/content
dotnet publish src/Piscine.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/pkg/piscine-win-x64
cp -r artifacts/content artifacts/pkg/piscine-win-x64/content
```
Expected : `artifacts/pkg/piscine-win-x64/piscine.exe` présent, `.../content/modules` présent, **aucun** `.../content/**/solution`. Vérifier l'exé : `./artifacts/pkg/piscine-win-x64/piscine.exe status`. Puis `Remove-Item -Recurse artifacts`.

- [ ] **Step 4 : Valider la syntaxe YAML**

Run: `python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/release.yml'))" ` (ou équivalent). Expected : pas d'erreur.

- [ ] **Step 5 : Commit**

```bash
git add build/launchers/start-piscine.cmd .github/workflows/release.yml
git commit -m "ci: release.yml publie les zips self-contained par OS sur tag

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `docs/mise-en-oeuvre.md`

**Files:**
- Create: `docs/mise-en-oeuvre.md`

- [ ] **Step 1 : Rédiger la doc** (spec §10.1) couvrant : prérequis réels (aucun SDK ; git fourni Windows via MinGit, système sous Linux/macOS) ; installation (télécharger le zip de la dernière release selon l'OS, dézipper, Windows : déblocage SmartScreen, Linux/macOS : `chmod +x piscine`) ; premier lancement (`piscine init` → workspace + dépôt bare + hook ; Windows : lancer via `start-piscine.cmd` pour avoir git sur PATH) ; boucle de travail (`piscine start <exo>`, `piscine check`, puis `git add/commit/push origin main`) ; dépannage (antivirus, chemins, `PISCINE_HOME`/`PISCINE_CONTENT`, réinitialisation) ; côté encadrant (check-list poste + remise du zip).

- [ ] **Step 2 : Lier depuis le README** (section « Mise en œuvre » → lien vers `docs/mise-en-oeuvre.md`), si le README a une table des matières.

- [ ] **Step 3 : Commit**

```bash
git add docs/mise-en-oeuvre.md README.md
git commit -m "docs: guide de mise en oeuvre (preparation poste recrue)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : Vérification finale + push + (option) première release

- [ ] **Step 1 : Suite Release + push main** (push en appel séparé)

```bash
dotnet test Piscine.slnx --configuration Release   # tous verts
git push origin main
gh run watch --exit-status                          # CI verte (release.yml inerte hors tag)
```

- [ ] **Step 2 : DEMANDER avant de taguer** — créer un tag `v0.1.0` déclenche une **vraie GitHub Release publique** (action sortante, difficile à défaire). Demander au propriétaire avant `git tag v0.1.0 && git push origin v0.1.0`. Si accordé : pousser le tag, `gh run watch`, vérifier les 3 zips attachés à la Release, et tester l'install d'après `docs/mise-en-oeuvre.md`.

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.6b) :** exclusion `solution/` testée (`ContentPackager`) + binaire renommé `piscine` (T1) ; commande `package-content` (T2) ; `release.yml` (publish dossier self-contained ×3 OS + content sans solution + MinGit Windows + lanceur + GitHub Release) + lanceur Windows (T3) ; doc de mise en œuvre (T4) ; vérif + release optionnelle sur accord (T5). ✓

**Vérifié hors-CI :** publish self-contained fonctionnel, grader io+unit OK depuis le dossier publié (de-risk fait avant le plan). **release.yml** non exécutable sans tag → logique publish/content/zip rejouée localement (T3 Step 3) ; le `gh release create` + download MinGit ne sont prouvés qu'au 1er tag réel (T5, sur accord).

**Reporté :** trimming/réduction de taille (105 Mo/OS — YAGNI) ; lanceur unix superflu (le binaire EST `piscine`). It.7 = Wiki GitHub ; It.8 = Module 00.
