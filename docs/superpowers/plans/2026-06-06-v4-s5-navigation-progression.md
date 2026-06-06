# v4 Sprint 5 — Navigation d'exercices + progression — Plan d'implémentation

> Issue #26 (milestone « v4 — application desktop Photino »). Branche : `v4/s5-navigation-progression`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md) §2 (« navigation
> d'exercices, statut par exercice »), §6 (flux : `check` instantané, rendu officiel git → `progress.json`,
> boucle de coaching). Plans précédents : [S1](2026-06-06-v4-s1-foundation.md), [S3](2026-06-06-v4-s3-git-coaching.md),
> [S4](2026-06-06-v4-s4-check-feedback.md). Dépend de **S1** (RCL / `CourseCatalog`) et **S3** (`GitStatusService`/`RepoState`).

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development ou superpowers:executing-plans,
> tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR (le parent ajoute le trailer
> `Co-Authored-By`). **Aucune nouvelle dépendance NuGet** : on réutilise `Piscine.Core` (`ProgressStore`/`Progress`/
> `ExerciseStatus`, `PiscineLayout`, déjà référencés par `Piscine.App`), `GitStatusService`/`RepoState` (S3),
> `CourseCatalog` (RCL S1), bUnit 2.7.2 + Playwright (S1), `GitFixtureBuilder` (dans `Piscine.Grading`, tests
> git uniquement). **Le moteur n'est PAS modifié** — `ProgressService` le LIT (`ProgressStore.Load`), il ne l'écrit pas.

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Point d'entrée moteur LU (pas modifié)** : `ProgressService` lit la progression persistée via
  `new ProgressStore(layout.ProgressPath).Load()` → `Progress.Exercises[exerciseId].Status`
  (`ExerciseStatus { NonCommence, ARevoir, Reussi }`). Il LIT aussi l'état git repo-wide via
  `GitStatusService.Read(layout.WorkspaceRoot)` → `RepoState`, et teste la présence de livrables dans
  `layout.WorkspaceExerciseDir(moduleId, exerciseId)`. **Aucune écriture** : pas de `Save`, pas de `check`,
  pas de progression mise à jour (c'est S4/le moteur qui écrit). ⚠️ **À VÉRIFIER au début de T1** : signatures
  exactes (`ProgressStore(string).Load()`, `Progress.Exercises`, `ExerciseStatus`, `PiscineLayout.ProgressPath`/
  `WorkspaceExerciseDir`/`WorkspaceRoot`, `RepoState.{AheadOfOrigin,HasOrigin,HasUncommittedWork,StagedCount,
  UnstagedCount,UntrackedCount}`) — adapter si une signature diffère.
- **(b) Statut dérivé, mapping décidé** : `progress.json` n'a que 3 valeurs et est écrit **à l'identique** par le
  `check` local et par `grade-received` (push officiel) → **`PousseNote` n'est pas distinguable de manière fiable
  d'un « réussi en local »**. Décision : statut **best-effort** combinant `progress.json` + `RepoState` repo-wide,
  avec un champ `Source` (`Progress`/`GitDerived`) pour l'honnêteté UI ; jamais de note. Règle de combinaison
  déterministe encodée dans `ProgressService.StatusFor` :

  | Statut issue | Source du signal (lecture seule, déterministe) |
  |---|---|
  | **NonCommence** | pas d'entrée `progress.json` ET pas de dossier workspace exo avec fichier. |
  | **EnCours** | fichiers workspace présents ET (pas d'entrée OU `RepoState.HasUncommittedWork`) ET pas encore `Reussi`. |
  | **CommiteNonPousse** | `RepoState.AheadOfOrigin > 0` (commits locaux non sur `origin`). |
  | **PousseNote** (best-effort) | entrée `Reussi` ET `AheadOfOrigin == 0` ET `HasOrigin`. |
  | **ARevoir** | entrée `progress.json` `Status == ARevoir`. |

- **(c) Git repo-wide** : le workspace de la recrue est **un seul dépôt** à `WorkspaceRoot`
  (`GitWorkspace.Initialize`), exos en sous-dossiers `<moduleId>/<exerciseId>/`. On lit `RepoState` **une fois** et
  on l'applique. **ÉCART D'IMPLÉMENTATION (revue S5)** : seul le signal **repo-wide** est appliqué (badge `GitDerived`
  + infobulle « best-effort » qui le disclose honnêtement) ; l'**attribution fine par préfixe de chemin** envisagée
  ici est **REPORTÉE en suivi** (nécessite que `GitStatusService` expose l'avance/le travail non committé par chemin,
  pas seulement repo-wide). Conséquence assumée : dès qu'un commit local est en avance, tous les exos `Reussi`/sans
  entrée concernés affichent `CommiteNonPousse`.
- **(d) Déterminisme tests** : `ProgressServiceTests` plante un `progress.json` réel via `ProgressStore.Save` + des
  fichiers workspace + des états git via `GitFixtureBuilder` (comme `GitStatusServiceTests`) → un `[Fact]` par statut
  dérivable. bUnit : badges construits à la main (composant pur). E2E : `PISCINE_CONTENT`/`PISCINE_WORKSPACE`/
  `PISCINE_HOME` pointant un workspace + state plantés → naviguer `/progress`, voir les badges.

**Goal:** Donner à la recrue une **vue de navigation + progression** : l'arbre modules → exercices avec un
**badge de statut par exo** (NonCommencé / EnCours / Commité-non-poussé / Poussé-noté / À revoir), un lien vers la
page de cours de l'exo (`/module/{id}/{exoId}`, S1) et vers `/check` (S4). Toute la logique de dérivation du statut
vit dans un service pur `ProgressService` (dans `Piscine.App`), qui **lit** le moteur (`ProgressStore`), l'état git
(`GitStatusService`/`RepoState`) et le workspace — **sans rien écrire**. Prouvé par xUnit (un cas par statut dérivable),
bUnit (le bon badge pour un statut donné) et E2E Playwright (naviguer → statuts visibles).

**Architecture:** Logique dans **`Piscine.App`** (sans UI ni Photino) → consommée à l'identique par Photino et le
harnais DevHost (invariant spec §4). `ProgressService` est **pur, lecture seule, déterministe** : il APPELLE
`ProgressStore`/`GitStatusService` (déjà référencés/dispo dans `Piscine.App`) — il ne les modifie pas. Le composant
de progression (`ProgressList.razor` + `StatusBadge.razor`) et la page (`ProgressPage.razor`, `@page "/progress"`,
`@rendermode InteractiveServer`) vivent dans la RCL `Piscine.Components` ; ils réutilisent `CourseCatalog` (S1) pour
l'arbre modules/exos et les styles de badges existants (`badge badge-*`, cf. `Module.razor`). Moteur
(`Core`/`Grading`/`Git`), `Piscine.Cli` et `release.yml` **INTACTS**.

**Tech Stack:** .NET 10 ; `Piscine.Core` (`ProgressStore`, `Progress`, `ExerciseStatus`, `PiscineLayout`) ;
`Piscine.App.Git` (`GitStatusService`, `RepoState`, S3) ; Blazor (RCL, `@rendermode InteractiveServer`) ;
`CourseCatalog` (RCL S1) ; xUnit 2.9.3 ; bUnit 2.7.2 (`BunitContext`/`Render<T>`) ; Playwright (skip-sans-Chromium,
racine via `Piscine.slnx`, parallélisation désactivée, port dédié) ; `GitFixtureBuilder` (tests git). Aucune nouvelle
référence NuGet.

---

## ⚠️ Note de risque (dériver 5 statuts distincts sans le résultat « grade-received » officiel)

**Risque principal** : l'issue demande **5 statuts** mais le signal persistant (`progress.json`) n'en a que **3**
(`NonCommence`, `ARevoir`, `Reussi`) et est écrit **identiquement** par le `check` local (`CheckCommand`) et par le
rendu officiel (`GradeReceivedCommand`). Il n'existe **aucun signal per-exercice de « poussé→noté »** distinct d'un
« réussi en local » dans le harnais DevHost (le hook `post-receive` tourne hors app). → On dérive un statut
**best-effort** en combinant `progress.json` + l'état git repo-wide (`RepoState.AheadOfOrigin`/`HasOrigin`/
`HasUncommittedWork`) + la présence de fichiers workspace, **selon une règle déterministe** encodée dans
`ProgressService.StatusFor`, avec un champ `Source` (`Progress` vs `GitDerived`) que l'UI expose honnêtement
(infobulle « statut déduit ») et **jamais de note**. **Où ça dégrade** : `PousseNote` n'est qu'un best-effort
(`Reussi` + rien en avance + `origin` présent) ; documenté dans le self-review et l'infobulle. Les tests rendent
chaque statut **dérivable** déterministe (`progress.json` planté + workspace planté + `GitFixtureBuilder`).

**Risque secondaire** : le dépôt git est **unique** (un seul `WorkspaceRoot`), pas un dépôt par exo → l'attribution
« commité-non-poussé » par exercice est best-effort (préfixe de chemin `<moduleId>/<exerciseId>/`). Si ambigu, repli
sur le signal repo-wide. **Aucune écriture git** (lecture seule via `GitStatusService`, déjà éprouvé S3).

## Carte des fichiers

- Créer : `src/Piscine.App/Progress/ExerciseProgressStatus.cs` (enum 5 valeurs : `NonCommence`, `EnCours`, `CommiteNonPousse`, `PousseNote`, `ARevoir` ; + `StatusSource { Progress, GitDerived }`)
- Créer : `src/Piscine.App/Progress/ExerciseStatusInfo.cs` (record UI : `ModuleId`, `ExerciseId`, `Status`, `Source` ; PUR)
- Créer : `src/Piscine.App/Progress/ProgressService.cs` (`StatusFor(moduleId, exoId)` + `SnapshotFor(...)` ; LIT `ProgressStore`/`GitStatusService`/workspace, ÉCRIT rien)
- Créer : `src/Piscine.Components/Components/Progress/StatusBadge.razor` (+ `.razor.css`) — badge par statut (libellé FR + classe + `data-testid`)
- Créer : `src/Piscine.Components/Components/Progress/ProgressList.razor` (+ `.razor.css`) — arbre modules→exos, badge par exo, liens cours + `/check`
- Créer : `src/Piscine.Components/Components/Progress/ProgressPage.razor` (`@page "/progress"`, `@rendermode InteractiveServer`)
- Modifier : `src/Piscine.Components/Components/Layout/NavMenu.razor` (lien « Progression » → `/progress` ; « Vérifier » → `/check`)
- Modifier : `src/Piscine.DevHost/Program.cs` (DI : `GitStatusService`/`PiscineLayout` déjà là ; ajouter `ProgressService`)
- Tests : `tests/Piscine.App.Tests/ProgressServiceTests.cs` (un cas par statut dérivable) ; réutilise `TempDir.cs`
- Test : `tests/Piscine.Components.Tests/StatusBadgeTests.cs` (bUnit) + (option) `ProgressListTests.cs`
- Test : `tests/Piscine.DevHost.E2E/ProgressSmokeTests.cs` (Playwright : `/progress` → arbre + badges)
- **NE PAS toucher** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`, `.github/workflows/release.yml`.

---

### Task 1 : `ProgressService` + modèle de statut dans `Piscine.App` (wrappe le moteur en lecture) + tests unitaires

**Files:**
- Create: `src/Piscine.App/Progress/ExerciseProgressStatus.cs`, `ExerciseStatusInfo.cs`, `ProgressService.cs`
- Test: `tests/Piscine.App.Tests/ProgressServiceTests.cs` ; réutilise `tests/Piscine.App.Tests/TempDir.cs`

- [ ] **Step 0 — Vérifier l'API moteur lue** : relire `src/Piscine.Core/Progression/ProgressStore.cs`,
  `Model/Progress.cs`, `Model/ExerciseStatus.cs`, `PiscineLayout.cs`, `src/Piscine.App/Git/GitStatusService.cs` +
  `RepoState.cs`. Confirmer/ajuster les signatures de la décision (a) avant de coder.

- [ ] **Step 1 — Enum + record UI purs**
  - `enum ExerciseProgressStatus { NonCommence, EnCours, CommiteNonPousse, PousseNote, ARevoir }` + `enum StatusSource { Progress, GitDerived }`.
  - `record ExerciseStatusInfo(string ModuleId, string ExerciseId, ExerciseProgressStatus Status, StatusSource Source)`. Aucune note.

- [ ] **Step 2 — Le service** `ProgressService.cs`. **Pur, lecture seule, déterministe**. Ctor
  `(PiscineLayout layout, GitStatusService git)` :
  - `StatusFor(moduleId, exerciseId)` : (1) `progress = new ProgressStore(_layout.ProgressPath).Load()` ;
    (2) `repo = _git.Read(_layout.WorkspaceRoot)` ; (3) `hasFiles` = dossier exo non vide ; (4) règle de combinaison :
    `ARevoir`→ARevoir(Progress) ; `Reussi` + `HasOrigin` + `AheadOfOrigin==0`→PousseNote(GitDerived), sinon
    CommiteNonPousse(GitDerived) ; pas d'entrée : `AheadOfOrigin>0`→CommiteNonPousse ; sinon `hasFiles||HasUncommittedWork`→EnCours ;
    sinon NonCommence. (Attribution fine par préfixe `<moduleId>/<exerciseId>/` best-effort ; repli repo-wide documenté.)
  - `IReadOnlyList<ExerciseStatusInfo> SnapshotFor(IEnumerable<(string ModuleId, string ExerciseId)> exos)` : charge
    `progress` et `repo` **une fois**, mappe chaque exo (évite N lectures). Méthode appelée par la page.

- [ ] **Step 3 — Tests unitaires** `ProgressServiceTests.cs` (squelette `CheckServiceTests`/`GitStatusServiceTests` :
  racine via `Piscine.slnx`, `TempDir`, `GitFixtureBuilder`). Un `[Fact]` par statut dérivable :
  - **NonCommence** : pas de `progress.json`, workspace vide, pas de dépôt → `NonCommence`.
  - **EnCours** : fichier dans `WorkspaceExerciseDir` (pas de progress) → `EnCours`.
  - **ARevoir** : `ProgressStore.Save` d'un `Progress` `Exercises[exoId].Status = ARevoir` → `ARevoir`.
  - **PousseNote (best-effort)** : `progress` `Reussi` + dépôt poussé vers un bare `origin` (`AheadOfOrigin==0`, `HasOrigin`) → `PousseNote`, `Source=GitDerived`.
  - **CommiteNonPousse** : `progress` `Reussi` + commit local en avance (`AheadOfOrigin==1`) → `CommiteNonPousse`.
  - **Dégradation verrouillée** : `Reussi` **sans** origin → `CommiteNonPousse` (et non `PousseNote`).

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning ; les ~6 cas PASS ; tests S3/S4 toujours verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): ProgressService (statut par exo derive de progress.json + RepoState + workspace, lecture seule) + modele + tests"
```

### Task 2 : composants `StatusBadge` + `ProgressList` (arbre + badges + liens) + bUnit

**Files:**
- Create: `src/Piscine.Components/Components/Progress/StatusBadge.razor` (+ `.razor.css`), `ProgressList.razor` (+ `.razor.css`)
- Test: `tests/Piscine.Components.Tests/StatusBadgeTests.cs` (+ option `ProgressListTests.cs`)

- [ ] **Step 1 — `StatusBadge.razor`** : `[Parameter] ExerciseProgressStatus Status` (+ optionnel `StatusSource Source`).
  Rend `<span class="badge status-@class" data-testid="status-badge" data-status="@Status">@Label</span>`. Libellés FR :
  `NonCommence`→« Non commencé » ; `EnCours`→« En cours » ; `CommiteNonPousse`→« Commité, non poussé » ;
  `PousseNote`→« Poussé → noté » ; `ARevoir`→« À revoir ». Si `Source == GitDerived`, ajouter
  `title="Statut déduit de l'état git (best-effort)"`. Réutiliser le look `badge` (`Module.razor`) ; CSS scopé
  auto-bundlé (pas de clé `@Assets`).

- [ ] **Step 2 — `ProgressList.razor`** : `[Parameter] IReadOnlyList<ExerciseStatusInfo> Statuses` + `@inject CourseCatalog Catalog`.
  Arbre `Catalog.Modules`→groupes→exos (modules avec exos) ; par exo : titre, `<StatusBadge Status="..." Source="..." />`
  (lookup par `(ModuleId,Id)`), lien `data-testid="exo-course-link"` `href="/module/@m.Id/@ex.Id"` et lien
  `data-testid="exo-check-link"` `href="/check"`. Conteneur racine `data-testid="progress-list"`.

- [ ] **Step 3 — bUnit** `StatusBadgeTests.cs` (`BunitContext`, `Render<T>()`). `[Theory]` sur les 5 statuts →
  `data-testid="status-badge"` présent, `TextContent` contient le libellé FR, `data-status` = nom de l'enum. Un cas
  `Source = GitDerived` → attribut `title` présent.

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: 0 warning ; `StatusBadgeTests` PASS ; `MarkdownViewTests`/`CheckFeedbackTests` toujours verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): composants StatusBadge + ProgressList (arbre modules/exos, badge par statut, liens cours et /check) + bUnit"
```

### Task 3 : page `/progress` (ProgressService → ProgressList) + lien NavMenu + DI

**Files:**
- Create: `src/Piscine.Components/Components/Progress/ProgressPage.razor` (`@page "/progress"`)
- Modify: `src/Piscine.Components/Components/Layout/NavMenu.razor`, `src/Piscine.DevHost/Program.cs`

- [ ] **Step 1 — DI dans le DevHost** : `GitStatusService` (S3) et `PiscineLayout` (S4, contrôlable par
  `PISCINE_CONTENT`/`PISCINE_WORKSPACE`/`PISCINE_HOME`) sont **déjà** enregistrés. Ajouter
  `builder.Services.AddSingleton(sp => new ProgressService(sp.GetRequiredService<PiscineLayout>(), sp.GetRequiredService<Piscine.App.Git.GitStatusService>()));`
  + `using` requis. Routage RCL déjà câblé (`AddAdditionalAssemblies(...MarkdownView.Assembly)`).

- [ ] **Step 2 — La page** `ProgressPage.razor` : `@page "/progress"`, `@rendermode InteractiveServer`,
  `@namespace Piscine.Components.Components.Progress`, `@inject CourseCatalog Catalog`, `@inject Piscine.App.Progress.ProgressService Progress`.
  `OnInitialized` : liste `(ModuleId, ExerciseId)` depuis `Catalog.Modules.SelectMany(m=>m.Groups).SelectMany(g=>g.Exercises)`,
  `Progress.SnapshotFor(...)`, passe à `<ProgressList Statuses="_statuses" />`. Titre « Progression ». Pas de garde
  dev-only (lecture seule).

- [ ] **Step 3 — NavMenu** : ajouter deux liens : « Progression » → `/progress` (`data-testid="nav-progress"`) et
  « Vérifier » → `/check` (non lié jusqu'ici, note S4). Réutiliser les classes existantes ; `active` via le helper `Segments`.

- [ ] **Step 4 — Vérif visuelle (preview)** : `dotnet run --project src/Piscine.DevHost --urls http://localhost:5254`
  (avec `PISCINE_CONTENT` vers `content/` ; workspace/state par défaut → tout `NonCommence`), `preview_start` sur
  `/progress`, vérifier l'arbre + badges « Non commencé », cliquer un lien cours → page exo. `preview_screenshot`.

- [ ] **Step 5 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: 0 warning ; `/progress` routable ; NavMenu lie `/progress` et `/check`.

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): page /progress (ProgressService -> ProgressList) + liens NavMenu (progression, verifier) + DI DevHost"
```

### Task 4 : E2E Playwright (naviguer → statuts visibles)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/ProgressSmokeTests.cs`

- [ ] **Step 1 — Le test** : squelette `CheckSmokeTests`/`CoachingSmokeTests` (port dédié 5255, poll, skip propre sans
  Chromium, racine via `Piscine.slnx`, kill arbre en `DisposeAsync`).
  - `InitializeAsync` : `PISCINE_HOME` → dossier temp isolé ; planter un `progress.json` (via `ProgressStore.Save` ou
    JSON littéral) avec un exo `Reussi` et un `ARevoir`, et/ou un fichier workspace ; fixer
    `PISCINE_CONTENT`/`PISCINE_WORKSPACE`/`PISCINE_HOME` sur le process `dotnet run`. Nettoyer en `DisposeAsync`.
  - Parcours : `GotoAsync("/progress")`, attendre `[data-testid="progress-list"]`, asserter `[data-testid="status-badge"]`
    `count > 0` et qu'au moins un badge porte le `data-status` planté (ex. `ARevoir`) ; un `[data-testid="exo-course-link"]` présent.

- [ ] **Step 2 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium en CI).

- [ ] **Step 3 — Commit**

```bash
git add -A
git commit -m "test(v4): E2E /progress (navigation modules/exos -> badges de statut visibles)"
```

### Task 5 : vérification globale + garde-fous (moteur intact) + PR

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** ; tous verts (204 S4 + `ProgressServiceTests` (~6) + `StatusBadgeTests` (5–6) ;
E2E se sautent proprement sans Chromium).

- [ ] **Step 2 — Garde-fous (moteur intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. `ProgressService` **lit** `ProgressStore`/`GitStatusService`, il ne les modifie pas.

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR** (commit et push en **appels séparés**)

```bash
git push -u origin v4/s5-navigation-progression
gh pr create --base main --title "v4 S5 — navigation d'exercices + progression" --body-file <fichier>
```

---

## Self-review (couverture S5 vs issue #26 / spec §2,§6)

- **Objectif** « naviguer modules/exercices et visualiser la progression » → arbre `ProgressList` (T2) sur
  `/progress` (T3) + lien NavMenu (T3). ✅
- **Périmètre** : liste modules/exos (réutilise `CourseCatalog`, S1) ✅ T2 · **statut par exo** (5 valeurs) dérivé par
  `ProgressService` ✅ T1 · navigation (liens cours S1 + `/check` S4) ✅ T2/T3 · progression visible ✅. **Ne
  réimplémente pas** `check` (S4) ni le coaching (S3).
- **Critères d'acceptation** : navigation complète ✅ T3+T4 · statut par exo correct (un `[Fact]`/statut dérivable,
  règle verrouillée) ✅ T1 · **bUnit** (bon badge) ✅ T2 · **E2E** (naviguer → statuts) ✅ T4. **Aucune note** ✅.
- **Dépendances** : **S1** (RCL, `CourseCatalog`, pyramide) ✅ · **S3** (`GitStatusService`/`RepoState`, déjà au DI) ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors 0 warning (T5) · moteur + `Cli` + `release.yml` **INTACTS**
  (`ProgressService` **lit** ; gate diff vide T5) · `@rendermode InteractiveServer` (T3) · routage RCL déjà câblé (S1) ·
  CSS scopé auto-bundlé, pas de clé `@Assets` (T2) · bUnit 2.x (`BunitContext`/`Render<T>`) (T2) · Playwright
  skip-sans-Chromium + racine `Piscine.slnx` + parallélisation désactivée + port 5255 (T4) · `GitFixtureBuilder`
  **uniquement** pour les états git (T1) · `PISCINE_CONTENT`/`PISCINE_WORKSPACE`/`PISCINE_HOME` pour déterminisme E2E.
- **Risque principal maîtrisé** : 5 statuts dérivés de 3 valeurs `progress.json` + git → règle déterministe dans
  `StatusFor`, champ `Source` + infobulle « best-effort », **dégradation explicite** de `PousseNote` verrouillée par
  un test. Git repo-wide → attribution par préfixe best-effort, repli documenté.
- **Déterminisme** : `progress.json` planté via `ProgressStore.Save`, workspace planté, états git via
  `GitFixtureBuilder`, `TempDir` + `PiscineLayout` explicite par test.
