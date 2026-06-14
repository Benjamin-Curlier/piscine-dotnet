# Harnais de test agentique (QA piloté par Claude) — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Donner à Claude un harnais pour piloter le DevHost dans des états déterministes (profils de seed) via le Playwright MCP existant, puis évaluer la qualité contre une rubrique et améliorer l'UI de façon autonome jusqu'à une barre.

**Architecture:** Un hook de démarrage dans le DevHost (hôte dev/test, NON livré) seede un `PISCINE_HOME` temporaire via les types réels (`Progress`/`ProgressStore`/`PiscineLayout`/`CourseCatalog`) selon un profil nommé (`PISCINE_QA_PROFILE`). Des scripts launcher (pwsh+bash) créent le temp, fixent les env vars et lancent le DevHost. Claude pilote via le Playwright MCP. Un skill encode la boucle évaluer→corriger→réévaluer + rubrique.

**Tech Stack:** .NET 10, Blazor (DevHost ASP.NET), Playwright MCP, xUnit/Playwright (E2E), skill markdown.

**Spec:** `docs/superpowers/specs/2026-06-14-agent-qa-harness-design.md`

**Invariants:** Modifs limitées à `src/Piscine.DevHost` (hôte test, non livré), `src/Piscine.Components` (ajouts `data-testid` additifs), `scripts/`, `tests/`, `.claude/skills/`. **Ne PAS toucher** `Piscine.Core`/`Grading`/`Git`/`GitShim`/`Cli`/`Sandbox*`/`Piscine.Desktop`/`Piscine.App`(logique métier)/`.github`. Commits conventionnels FR + trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`. commit ≠ push. Branche : `feat/agent-qa-harness` (déjà créée, spec déjà commitée dessus).

---

## Fichiers

| Fichier | Rôle |
|---|---|
| `src/Piscine.DevHost/Qa/QaProfile.cs` | **Créer.** Enum/constantes des profils + parse depuis string. |
| `src/Piscine.DevHost/Qa/QaSeeder.cs` | **Créer.** Seede l'état déterministe d'un profil via types réels. |
| `src/Piscine.DevHost/Program.cs` | **Modifier.** Au démarrage, si `PISCINE_QA_PROFILE` défini → `QaSeeder.Seed(...)`. |
| `scripts/devhost-qa.ps1` | **Créer.** Launcher Windows : temp HOME + env + `dotnet run` DevHost. |
| `scripts/devhost-qa.sh` | **Créer.** Launcher Linux/macOS (parité CI). |
| `src/Piscine.Components/**` (ciblé) | **Modifier.** Ajouts `data-testid` manquants (additifs). |
| `tests/Piscine.DevHost.E2E/QaProfileSmokeTests.cs` | **Créer.** Smoke : chaque profil démarre et rend son testid emblématique (port 5283). |
| `.claude/skills/qa-and-refine/SKILL.md` | **Créer.** Workflow boucle + rubrique + garde-fous. |

---

## Task 1 : Profils + seeder (`fresh` et `mixed`) + hook DevHost

**Files:**
- Create: `src/Piscine.DevHost/Qa/QaProfile.cs`
- Create: `src/Piscine.DevHost/Qa/QaSeeder.cs`
- Modify: `src/Piscine.DevHost/Program.cs`

- [ ] **Step 1 : Lire les dépendances réelles** — ouvrir et confirmer la surface utilisée :
  `src/Piscine.Core/Model/Progress.cs` (`Progress.Exercises`, `ExerciseProgress { Status, Attempts, LastAttempt }`),
  `src/Piscine.Core/Progression/ProgressStore.cs` (`new ProgressStore(path).Save(progress)`),
  `src/Piscine.Core/PiscineLayout.cs` (`FromEnvironment()`, `ProgressPath`, `StateDir`, `LastPushResultPath`),
  l'enum **`ExerciseStatus`** (noter ses membres exacts — `ARevoir` existe ; relever celui pour « réussi/fait » et « en cours » et « non démarré »),
  `CourseCatalog` (comment lister modules/exercices + la forme des identifiants d'exercice, ex. `ex00-hello`),
  et comment **`InitService.Status`** détermine qu'un workspace est « initialisé » (signal à poser/retirer pour piloter l'overlay onboarding). Adapter les noms ci-dessous aux membres réels.

- [ ] **Step 2 : Créer `QaProfile.cs`**

```csharp
namespace Piscine.DevHost.Qa;

/// <summary>Profils de seed déterministes pour la QA agentique (cf. spec §3.1).</summary>
public enum QaProfile
{
    Fresh,       // workspace NON initialisé → overlay onboarding
    Mixed,       // initialisé, progression variée
    ExoFail,     // un exo avec dernier check en échec (diff)
    ExoPass,     // un exo réussi
    PushResult,  // last-push-result.json récent (toast + /resultat)
    Done,        // quasi tout terminé (rapport significatif)
}

public static class QaProfiles
{
    /// <summary>Parse insensible à la casse/format ("exo-fail" → ExoFail). Null si inconnu.</summary>
    public static QaProfile? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var k = raw.Trim().Replace("-", "").Replace("_", "");
        return k.ToLowerInvariant() switch
        {
            "fresh" => QaProfile.Fresh,
            "mixed" => QaProfile.Mixed,
            "exofail" => QaProfile.ExoFail,
            "exopass" => QaProfile.ExoPass,
            "pushresult" => QaProfile.PushResult,
            "done" => QaProfile.Done,
            _ => null,
        };
    }
}
```

- [ ] **Step 3 : Créer `QaSeeder.cs` (profils `Fresh` et `Mixed` d'abord)** — utiliser les types réels confirmés au Step 1. Le seeder est idempotent (réécrit l'état). `Fresh` = état vierge + s'assurer que le signal d'init est ABSENT. `Mixed` = poser le signal d'init + une progression variée bâtie depuis `CourseCatalog`.

```csharp
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Components.Services; // CourseCatalog

namespace Piscine.DevHost.Qa;

/// <summary>
/// Seede un état déterministe dans le PISCINE_HOME courant (résolu par PiscineLayout.FromEnvironment).
/// Réservé au DevHost (hôte dev/test, non livré) ; activé via PISCINE_QA_PROFILE.
/// </summary>
public static class QaSeeder
{
    public static void Seed(QaProfile profile, PiscineLayout layout, CourseCatalog catalog)
    {
        Directory.CreateDirectory(layout.StateDir);
        Directory.CreateDirectory(layout.WorkspaceRoot);

        // Repartir d'un état propre à chaque démarrage (déterminisme).
        SafeDelete(layout.ProgressPath);
        SafeDelete(layout.LastPushResultPath);

        switch (profile)
        {
            case QaProfile.Fresh:
                EnsureUninitialized(layout);          // overlay onboarding
                break;

            case QaProfile.Mixed:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildMixedProgress(catalog));
                break;

            // Les autres profils sont ajoutés en Task 3.
            default:
                EnsureInitialized(layout);
                break;
        }
    }

    // Progression variée : sur les N premiers exercices du catalogue, alterner Fait/EnCours/ARevoir.
    private static Progress BuildMixedProgress(CourseCatalog catalog)
    {
        var progress = new Progress();
        var ids = EnumerateExerciseIds(catalog).Take(18).ToList();
        for (var i = 0; i < ids.Count; i++)
        {
            var status = (i % 3) switch
            {
                0 => /* membre réel « réussi/fait » */ ExerciseStatus.Reussi,
                1 => ExerciseStatus.EnCours,
                _ => ExerciseStatus.ARevoir,
            };
            progress.Exercises[ids[i]] = new ExerciseProgress { Status = status, Attempts = i % 3 + 1 };
        }
        return progress;
    }

    // À ADAPTER au Step 1 : énumère les identifiants d'exercice depuis CourseCatalog dans l'ordre.
    private static IEnumerable<string> EnumerateExerciseIds(CourseCatalog catalog) =>
        throw new NotImplementedException("Remplacer par l'énumération réelle des exos du CourseCatalog (Step 1).");

    // À ADAPTER : poser/retirer le signal d'init lu par InitService.Status (Step 1).
    private static void EnsureInitialized(PiscineLayout layout) =>
        throw new NotImplementedException("Poser le signal d'init réel (Step 1).");
    private static void EnsureUninitialized(PiscineLayout layout) =>
        throw new NotImplementedException("Retirer le signal d'init réel (Step 1).");

    private static void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }
}
```

> NOTE : les 3 `NotImplementedException` ci-dessus sont des points à compléter avec l'API réelle relevée au Step 1 (énumération `CourseCatalog`, membres `ExerciseStatus`, signal `InitService`). Ne PAS livrer avec ces `throw` — les remplacer par le code réel avant le commit du Step 6.

- [ ] **Step 4 : Brancher le hook dans `Program.cs`** — lire d'abord `src/Piscine.DevHost/Program.cs` pour situer la construction des services (où `CourseCatalog`/`PiscineLayout` sont enregistrés). Après `var app = builder.Build();` (avant `app.Run()`), ajouter :

```csharp
        // QA agentique (dev/test uniquement) : seede un état déterministe selon PISCINE_QA_PROFILE.
        var qaProfile = Piscine.DevHost.Qa.QaProfiles.Parse(
            Environment.GetEnvironmentVariable("PISCINE_QA_PROFILE"));
        if (qaProfile is { } p)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;
            Piscine.DevHost.Qa.QaSeeder.Seed(
                p,
                sp.GetRequiredService<Piscine.Core.PiscineLayout>(),
                sp.GetRequiredService<Piscine.Components.Services.CourseCatalog>());
        }
```

> Adapter les types injectés aux enregistrements réels (le DevHost enregistre déjà `PiscineLayout` et `CourseCatalog`, comme le Desktop).

- [ ] **Step 5 : Build**

Run: `dotnet build src/Piscine.DevHost -c Release`
Expected: Build succeeded, 0 Warning (après remplacement des `NotImplementedException`).

- [ ] **Step 6 : Commit**

```bash
git add src/Piscine.DevHost/Qa/QaProfile.cs src/Piscine.DevHost/Qa/QaSeeder.cs src/Piscine.DevHost/Program.cs
git commit -m "feat(qa): seeder de profils DevHost (fresh/mixed) + hook PISCINE_QA_PROFILE

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2 : Scripts launcher (`devhost-qa`)

**Files:**
- Create: `scripts/devhost-qa.ps1`
- Create: `scripts/devhost-qa.sh`

- [ ] **Step 1 : Créer `scripts/devhost-qa.ps1`**

```powershell
#requires -version 7
[CmdletBinding()]
param(
  [Parameter(Mandatory)] [ValidateSet('fresh','mixed','exo-fail','exo-pass','push-result','done')] [string]$Profile,
  [int]$Port = 5240
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot\..").Path
$home = Join-Path ([System.IO.Path]::GetTempPath()) "piscine-qa-$Profile-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Force -Path (Join-Path $home 'workspace') | Out-Null
$env:PISCINE_HOME      = $home
$env:PISCINE_WORKSPACE = Join-Path $home 'workspace'
$env:PISCINE_CONTENT   = Join-Path $repo 'content'
$env:PISCINE_QA_PROFILE = $Profile
Write-Host "[devhost-qa] profil=$Profile  home=$home  url=http://localhost:$Port/"
try {
  & dotnet run --project (Join-Path $repo 'src/Piscine.DevHost') --urls "http://localhost:$Port"
} finally {
  Remove-Item -Recurse -Force $home -ErrorAction SilentlyContinue
}
```

- [ ] **Step 2 : Créer `scripts/devhost-qa.sh`**

```bash
#!/usr/bin/env bash
set -euo pipefail
profile="${1:?usage: devhost-qa.sh <fresh|mixed|exo-fail|exo-pass|push-result|done> [port]}"
port="${2:-5240}"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
home="$(mktemp -d "${TMPDIR:-/tmp}/piscine-qa-${profile}-XXXXXX")"
mkdir -p "$home/workspace"
export PISCINE_HOME="$home" PISCINE_WORKSPACE="$home/workspace" PISCINE_CONTENT="$repo/content" PISCINE_QA_PROFILE="$profile"
echo "[devhost-qa] profil=$profile home=$home url=http://localhost:$port/"
cleanup() { rm -rf "$home"; }
trap cleanup EXIT
dotnet run --project "$repo/src/Piscine.DevHost" --urls "http://localhost:$port"
```

- [ ] **Step 3 : Vérifier manuellement `fresh` et `mixed`** — lancer chaque profil, confirmer le rendu attendu, puis arrêter (Ctrl-C). Via le Playwright MCP : `browser_navigate http://localhost:5240/` puis `browser_snapshot`.
  - `fresh` → l'overlay onboarding est présent.
  - `mixed` → le tableau de bord rend des pastilles de statut (plusieurs statuts).

- [ ] **Step 4 : Commit**

```bash
git add scripts/devhost-qa.ps1 scripts/devhost-qa.sh
git commit -m "feat(qa): scripts launcher devhost-qa (pwsh + bash) par profil

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3 : Profils restants (`exo-fail`, `exo-pass`, `push-result`, `done`)

**Files:**
- Modify: `src/Piscine.DevHost/Qa/QaSeeder.cs`

- [ ] **Step 1 : Localiser le format du résultat riche** — trouver le modèle/sérialiseur que `grade-received` écrit dans `last-push-result.json` et que `IPushResultWatcher`/`/resultat` (S11) lisent (chercher `last-push-result`, `LastPushResult`, `IPushResultWatcher`). **Réutiliser ce modèle** pour le profil `push-result` — NE PAS coder un JSON en dur.

- [ ] **Step 2 : Compléter `QaSeeder.Seed`** — remplacer le `default:` par les 4 cas, en réutilisant les helpers du Task 1 et le modèle riche du Step 1 :

```csharp
            case QaProfile.ExoPass:
                EnsureInitialized(layout);
                SaveSingle(layout, FirstExerciseId(catalog), ExerciseStatus.Reussi, attempts: 1);
                break;

            case QaProfile.ExoFail:
                EnsureInitialized(layout);
                SaveSingle(layout, FirstExerciseId(catalog), ExerciseStatus.ARevoir, attempts: 2);
                // Écrire un last-push-result.json « échec » via le modèle riche réel (Step 1)
                // pour que /resultat et le diff structuré aient de quoi rendre.
                WriteRichResult(layout, success: false, catalog);
                break;

            case QaProfile.PushResult:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildMixedProgress(catalog));
                WriteRichResult(layout, success: true, catalog); // toast + /resultat
                break;

            case QaProfile.Done:
                EnsureInitialized(layout);
                new ProgressStore(layout.ProgressPath).Save(BuildAllReussi(catalog));
                break;
```

Ajouter les helpers (`SaveSingle`, `FirstExerciseId`, `BuildAllReussi`, `WriteRichResult`) en réutilisant `ProgressStore`, `CourseCatalog` et le modèle riche réel. `WriteRichResult` sérialise le modèle réel vers `layout.LastPushResultPath`.

- [ ] **Step 3 : Build**

Run: `dotnet build src/Piscine.DevHost -c Release`
Expected: 0 Warning, 0 Error.

- [ ] **Step 4 : Vérifier `exo-fail` et `push-result`** via le Playwright MCP : `exo-fail` → la page d'exercice montre le diff coloré ; `push-result`/`/resultat` → résultat riche rendu, toast présent.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.DevHost/Qa/QaSeeder.cs
git commit -m "feat(qa): profils exo-fail/exo-pass/push-result/done (modèle riche réutilisé)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4 : Couverture `data-testid` (audit ciblé)

**Files:**
- Modify: `src/Piscine.Components/**` (uniquement les composants ciblés par la QA)

- [ ] **Step 1 : Auditer** — pour chaque écran de la rubrique (tableau de bord, cours, page d'exercice, /check, /resultat, /rapport, /reglages, onboarding, palette ⌘K), lister via `browser_snapshot` les éléments interactifs/zones clés **sans `data-testid`** stable. Dresser la liste des manquants.

- [ ] **Step 2 : Ajouter les `data-testid` manquants** — édition additive, nommage cohérent avec l'existant (`nav-*`, `status-dot`, `dashboard`, `module-grid`, …). Exemple de motif (déjà utilisé dans le repo) :

```razor
<button class="..." data-testid="check-run" @onclick="...">Vérifier</button>
```

Ne modifier QUE l'attribut/markup nécessaire ; ne pas changer la logique des composants.

- [ ] **Step 3 : Build + tests composants**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test tests/Piscine.Components.Tests -c Release`
Expected: 0 Warning ; tests bUnit verts (mettre à jour un test bUnit si une assertion de testid existante est concernée).

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Components
git commit -m "test(qa): compléter la couverture data-testid pour le pilotage QA

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5 : Smoke des profils (E2E)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/QaProfileSmokeTests.cs`

- [ ] **Step 1 : Écrire le test** — modelé sur `NavigationSmokeTests.cs` mais en passant `PISCINE_QA_PROFILE` (au lieu de seeder le progress.json en C#). Port dédié **5283** (distinct de 5247/5249/5251/5253/5255/5257/5259/5261/5263/5265/5267/5269/5271/5273/5275/5277/5281). Skip-sans-Chromium. Théorie xUnit sur les profils, chacun asserte son testid emblématique.

```csharp
using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

// Smoke : chaque profil QA démarre le DevHost dans son état et rend son testid emblématique.
// Port dédié 5283. Skip propre sans Chromium.
public sealed class QaProfileSmokeTests
{
    private const int Port = 5283;

    [Theory]
    [InlineData("fresh", "[data-testid='onboarding']")]   // overlay onboarding
    [InlineData("mixed", "[data-testid='status-dot']")]   // pastilles de progression
    public async Task Profile_boots_into_expected_state(string profile, string hallmark)
    {
        var repoRoot = FindRepoRoot();
        var home = Path.Combine(Path.GetTempPath(), $"piscine-qa-{profile}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(home, "workspace"));
        var url = $"http://localhost:{Port}";

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{Path.Combine(repoRoot, "src", "Piscine.DevHost")}\" --urls {url}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_HOME"] = home;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = Path.Combine(home, "workspace");
        psi.EnvironmentVariables["PISCINE_CONTENT"] = Path.Combine(repoRoot, "content");
        psi.EnvironmentVariables["PISCINE_QA_PROFILE"] = profile;

        using var host = Process.Start(psi) ?? throw new InvalidOperationException("DevHost KO.");
        try
        {
            await WaitForServerAsync(url, TimeSpan.FromSeconds(90));
            using var pw = await Playwright.CreateAsync();
            IBrowser browser;
            try { browser = await pw.Chromium.LaunchAsync(); }
            catch (PlaywrightException) { return; } // skip sans Chromium
            await using (browser)
            {
                var page = await browser.NewPageAsync();
                await page.GotoAsync(url, new() { Timeout = 30_000 });
                await page.WaitForSelectorAsync(hallmark, new() { Timeout = 30_000 });
            }
        }
        finally
        {
            try { host.Kill(entireProcessTree: true); } catch { }
            try { Directory.Delete(home, recursive: true); } catch { }
        }
    }

    private static async Task WaitForServerAsync(string url, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try { if ((await http.GetAsync(url)).IsSuccessStatusCode) return; }
            catch (HttpRequestException) { } catch (TaskCanceledException) { }
            await Task.Delay(500);
        }
        throw new TimeoutException($"DevHost muet sur {url}.");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Racine introuvable.");
    }
}
```

> Vérifier le testid réel de l'overlay onboarding (`OnboardingFlow.razor`) ; si absent, l'ajouter en Task 4 (`data-testid='onboarding'`).

- [ ] **Step 2 : Lancer le smoke**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release --filter "FullyQualifiedName~QaProfileSmokeTests"`
Expected: PASS (ou skip propre sans Chromium).

- [ ] **Step 3 : Commit**

```bash
git add tests/Piscine.DevHost.E2E/QaProfileSmokeTests.cs
git commit -m "test(qa): smoke de démarrage par profil (fresh/mixed, port 5283)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6 : Skill « qa-and-refine » (workflow + rubrique + garde-fous)

**Files:**
- Create: `.claude/skills/qa-and-refine/SKILL.md`

- [ ] **Step 1 : Écrire le skill** — un runbook que Claude suit pour exécuter la boucle. Doit contenir : (a) comment lancer un profil (`scripts/devhost-qa`), (b) la matrice profil × route × thème (clair/sombre) × largeur (1280/1024/768/420), (c) les outils Playwright MCP à utiliser (`browser_navigate`, `browser_resize`, `browser_take_screenshot`, `browser_snapshot`, `browser_evaluate` pour `console.error`), (d) la **rubrique** (spec §3.4), (e) les **garde-fous** (correctifs jetons-only, ≤3 itérations/zone, flag-don't-impose, build/tests/smoke verts), (f) le format du **rapport avant/après**.

```markdown
---
name: qa-and-refine
description: Piloter le DevHost (via le Playwright MCP) dans des états seedés, juger la qualité contre une rubrique, corriger dans les jetons existants, réévaluer jusqu'à une barre, produire un rapport avant/après. Déclencheurs : "passe QA", "qa-and-refine", "juge la qualité de l'app", "améliore l'UI".
---

# QA-and-refine (boucle d'amélioration pilotée)

## Lancer un état
`pwsh scripts/devhost-qa.ps1 -Profile <fresh|mixed|exo-fail|exo-pass|push-result|done> -Port 5240`
(ou `scripts/devhost-qa.sh <profil> 5240`). Attendre l'URL, puis piloter via le Playwright MCP.

## Matrice de capture
Pour chaque **profil** pertinent × **route** (/, /cours, page d'exercice, /check, /resultat, /rapport,
/reglages, palette ⌘K) × **thème** (clair, sombre) × **largeur** (1280, 1024, 768, 420) :
1. `browser_navigate` (route) ; basculer le thème ; `browser_resize` (largeur).
2. `browser_take_screenshot` (consigner le nom : `<profil>-<route>-<theme>-<largeur>.png`).
3. `browser_evaluate` → relever `console.error`/avertissements.
4. `browser_snapshot` → vérifier l'arbre a11y (focus, rôles, libellés).

## Rubrique (barre)
- Zéro erreur console / aucune Blazor error UI.
- Pas de débordement/rognage ni scrollbar inattendue aux largeurs cibles.
- Parité mode sombre + contraste AA.
- `:focus-visible` + navigation clavier sur tous les contrôles.
- États vide/chargement/erreur présents et stylés.
- Cohérence : variables CSS, pas de valeurs one-off.
- Flux principal sans impasse.

## Boucle
1. Capturer + noter (consigner chaque constat avec sa capture).
2. Corriger **uniquement dans `piscine.css` / CSS de composants** (jetons). Pas de refonte, pas de dépendance,
   pas de moteur/CLI/grade-received.
3. Rebuild (`dotnet build Piscine.slnx -c Release`, 0 warning) ; garder bUnit/E2E/smoke verts.
4. Recapturer la zone ; comparer avant/après. ≤ 3 itérations par zone.
5. Tout constat **subjectif** (goût, pas « cassé ») → **signalé** dans le rapport, pas corrigé d'office.

## Sortie
Rapport `docs/superpowers/audits/AAAA-MM-JJ-qa-pass.md` : galerie avant/après, constats résolus (P1/P2/P3),
constats signalés (subjectifs/différés). PR(s) de refonte scopées, chacune avec captures avant/après.
```

- [ ] **Step 2 : Commit**

```bash
git add .claude/skills/qa-and-refine/SKILL.md
git commit -m "feat(qa): skill qa-and-refine (rubrique + boucle évaluer→corriger→réévaluer)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7 : Vérification du harnais + PR

- [ ] **Step 1 : Build Release 0 warning** — `dotnet build Piscine.slnx -c Release`.
- [ ] **Step 2 : Tests** — `dotnet test Piscine.slnx -c Release --filter "FullyQualifiedName!~DesktopRenderSmokeTests"` verts ; smoke profils inclus.
- [ ] **Step 3 : Sanity Playwright MCP** — lancer `mixed`, `browser_navigate`, `browser_take_screenshot`, confirmer une capture exploitable.
- [ ] **Step 4 : Push + PR**

```bash
git push -u origin feat/agent-qa-harness
gh pr create --base main --title "feat(qa): harnais de test agentique (profils de seed + skill qa-and-refine)" --body "..."
```
Corps FR : profils, launcher, data-testid, smoke, skill ; terminer par `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

---

## Task 8 (exécution) : Lancer la boucle QA autonome → refonte

> C'est la livraison « do a new pass ». À faire APRÈS merge du harnais (Tasks 1–7).

- [ ] **Step 1** : Suivre le skill `qa-and-refine` sur tous les profils (boucle autonome jusqu'à la barre).
- [ ] **Step 2** : Produire le rapport `docs/superpowers/audits/2026-06-14-qa-pass.md` (avant/après).
- [ ] **Step 3** : Ouvrir la/les PR(s) de refonte (jetons-only, captures avant/après), auto-merge on green.
- [ ] **Step 4** : Spot-check natif mince (chrome de fenêtre + terminal) : sonde de rendu + computer-use si accès accordé, sinon checklist manuelle dans le rapport.

---

## Phase 2 (hors de ce plan, seulement si la Phase 1 est trop fragile)

Serveur MCP custom exposant les actions sémantiques éprouvées (`seed_state(profil)`, `goto(route|exo)`,
`run_check()`, `read_progress()`, `screenshot()`, `list_routes()`) — à spécifier dans un plan dédié.
