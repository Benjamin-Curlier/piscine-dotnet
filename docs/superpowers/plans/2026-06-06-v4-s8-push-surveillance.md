# v4 Sprint 8 — Surveillance du résultat de push (`grade-received`) — Plan d'implémentation

> Issue #29 (milestone « v4 — application desktop Photino »). Branche : `v4/s8-push-surveillance`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md) §6
> (« rendu officiel » : après `git push`, le hook `post-receive` lance `grade-received` headless ; l'app
> affiche le verdict éducatif **sans action manuelle**). Plans précédents :
> [S3](2026-06-06-v4-s3-git-coaching.md) (FORMAT du canal/événement + page `/terminal` où le push a lieu),
> [S4](2026-06-06-v4-s4-check-feedback.md) (rendu du feedback riche — `/check`),
> [S5](2026-06-06-v4-s5-navigation-progression.md) (`ProgressService` + `StatusBadge`, lecture de `progress.json`),
> [S7](2026-06-06-v4-s7-init-setup.md) (hook + isolation `PISCINE_HOME`/`PISCINE_WORKSPACE` en E2E).

> **For agentic workers:** REQUIRED SUB-SKILL : superpowers:subagent-driven-development ou
> superpowers:executing-plans, tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR
> (le parent ajoute le trailer `Co-Authored-By`). **Aucune nouvelle dépendance** : `FileSystemWatcher`
> (BCL), `ProgressStore`/`Progress` (Core, déjà référencés par `Piscine.App`), `StatusBadge`/`ProgressService`
> (S5), bUnit 2.7.2 + Playwright (S1). **Le grader headless n'est PAS modifié** — la surveillance LIT l'artefact.

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Ce que `grade-received` persiste — LE point dont tout dépend (vérifié dans le code).**
  `Piscine.Git.GradeReceivedCommand.Run(sha)` corrige le commit poussé puis, dans `Persist(...)`, **écrit un
  seul artefact durable : `progress.json`** via `ProgressStore.Save` à `PiscineLayout.ProgressPath`
  (= `{StateDir}/progress.json`, `StateDir = {PISCINE_HOME}/.state`). `ProgressRecorder.Apply` n'y inscrit, par
  exercice, que `Status` (`Reussi`/`ARevoir`), `Attempts++` et `LastAttempt` (timestamp). **Le verdict riche**
  (messages par grader, diff `Attendu/Obtenu`, indice, `course_ref`) est construit par `ResultFormatter.Format`
  dans `CommandResult.Output` et **seulement imprimé sur stdout** (`Piscine.Cli/Program.cs`) → sous le hook,
  part dans le side-band de git et est **perdu**, jamais persisté. (Confirmé par grep : seule écriture sous le
  state dir = `ProgressStore.Save`.)
- **(b) Décision honnête : rendu STATUT-ONLY + pont vers `/check`.** Comme seul `progress.json` est durable, S8
  surveille `progress.json` et rend le **verdict de statut** (réussi/à revoir + `attempts` + `lastAttempt`) par
  exercice changé, en **réutilisant `StatusBadge` (S5)** — **PAS** `CheckFeedback` (S4), dont le `CheckOutcome`
  (diff riche) est **impossible à reconstruire** depuis `progress.json`. Pour le diff riche, lien d'action vers
  `/check` (S4 re-corrige in-process). Limite assumée.
- **(c) Seam moteur signalé (NON implémenté en S8).** Rendre le verdict riche **sans** re-jouer exigerait que
  `GradeReceivedCommand.Persist` persiste aussi le résultat riche (ex. `last-push-result.json`). C'est une
  **modification du comportement du grader headless** (« inchangé » spec) → **hors périmètre** ; ouvrir une
  **issue de suivi** (T5 Step 5). Aucune modif moteur dans S8.
- **(d) Mécanisme = `FileSystemWatcher` + debounce + événement** (calqué sur `ICoachingChannel` S3). Relit
  `progress.json` (vérité) à chaque settle et **diffe** contre un snapshot → ne déclenche que sur changement
  réel ; un événement manqué/dupliqué est inoffensif (relecture complète).
- **(e) Signal « rendu en cours / terminé »** : enum `PushPhase { Idle, EnAttente, Recu }`.
- **(f) Top 3 risques** : 1) ce que persiste `grade-received` (cf. (a)) → statut-only + lien `/check` + seam
  signalé ; 2) fiabilité `FileSystemWatcher` (multi-événements, thread de fond, fuite) → debounce 250 ms +
  relecture + snapshot-diff + `InvokeAsync` + `IAsyncDisposable` ; 3) E2E sans vrai push → écrire `progress.json`
  via `ProgressStore.Save` dans le `PISCINE_HOME` isolé (l'API du hook) après chargement de la page.

**Goal:** Après un `git push`, **afficher le verdict éducatif sans action manuelle**. `PushResultWatcher`
(`Piscine.App`) surveille l'artefact durable du hook (`progress.json`, **grader headless inchangé**), relit la
vérité, en dérive le delta (exercices passés en Réussi/À revoir + timestamp) et **publie un événement** auquel
une page Blazor s'abonne pour s'auto-rafraîchir. Le rendu réutilise `StatusBadge` (S5) + un lien vers `/check`
(S4) pour le diff riche. Prouvé par xUnit (écrire l'artefact → événement/`LatestResult`), bUnit (rendu) et E2E
Playwright (artefact écrit → page rend le verdict, aucun clic).

**Architecture:** Logique dans **`Piscine.App`** (sans UI ni Photino), consommée à l'identique par Photino et le
harnais DevHost (spec §4). `PushResultWatcher` **LIT** `progress.json` via `ProgressStore` (Core, déjà référencé)
— il n'écrit rien et **ne touche pas le grader**. Le rendu (`PushResultPanel.razor`, `@page "/resultat"`,
`@rendermode InteractiveServer`) vit dans la RCL `Piscine.Components` et réutilise `StatusBadge` (S5). Moteur
(`Core`/`Grading`/`Git`), `Piscine.Cli` et `release.yml` **INTACTS** — `grade-received` tourne **inchangé**.

**Tech Stack:** .NET 10 ; `System.IO.FileSystemWatcher` (BCL) ; `ProgressStore`/`Progress`/`ExerciseProgress`/
`ExerciseStatus` (`Piscine.Core`) ; `StatusBadge`/`ExerciseProgressStatus` (S5) ; Blazor (RCL,
`@rendermode InteractiveServer`) ; xUnit 2.9.3 ; bUnit 2.7.2 (`BunitContext`/`Render<T>`) ; Playwright
(skip-sans-Chromium, racine via `Piscine.slnx`, parallélisation désactivée, **port dédié 5261**). **Aucune
nouvelle référence NuGet.**

---

## ⚠️ Note de risque

**Risque n°1 — ce que `grade-received` persiste.** Vérifié : **uniquement `progress.json`** (statut + attempts
+ lastAttempt). Le verdict riche n'est que sur stdout du hook → **perdu**. → S8 rend le **verdict de statut** via
`StatusBadge` (S5) + **lien `/check`** pour le diff riche ; **on ne modifie PAS** le grader. Le seam « persister
aussi le résultat riche » est **signalé en suivi**, non implémenté.

**Risque n°2 — fiabilité `FileSystemWatcher`** : émet plusieurs événements par écriture, sur un thread de fond,
peut en rater, fuit si mal disposé. → **debounce** (timer 250 ms), **relecture complète** de `progress.json` à
chaque settle + **snapshot-diff** (événement manqué/dupliqué inoffensif), `InvokeAsync` vers le circuit,
`IAsyncDisposable` + désabonnement page. Surveiller le **dossier** (`StateDir`) `Filter="progress.json"`,
`NotifyFilter = LastWrite | FileName`, gérer `Created`+`Changed`+`Renamed` ; **créer `StateDir`** avant de
watcher (sinon FSW lève).

**Risque secondaire — E2E sans vrai push** : piloter l'artefact via
`new ProgressStore(<home>/.state/progress.json).Save(progress)` (l'API du hook, format garanti — comme
`ProgressSmokeTests`) **après** chargement de `/resultat`, asserter le rendu **auto**. Isolation
`PISCINE_HOME`/`PISCINE_WORKSPACE` jetable + `ClearReadOnly`/`Directory.Delete` en `DisposeAsync`.

## Carte des fichiers

- Créer : `src/Piscine.App/Push/PushResult.cs` (record : exercices changés + statut + timestamp ; enum `PushPhase` ; PUR)
- Créer : `src/Piscine.App/Push/IPushResultWatcher.cs` (interface : `event ResultReceived`, `LatestResult()`, `Start()`, `IAsyncDisposable`)
- Créer : `src/Piscine.App/Push/ProgressFileWatcher.cs` (impl `FileSystemWatcher` + debounce + snapshot-diff via `ProgressStore` ; LIT seulement)
- Créer : `src/Piscine.Components/Components/Push/PushResultPanel.razor` (`@page "/resultat"`, `@rendermode InteractiveServer`) + `.razor.css`
- Modifier : `src/Piscine.Components/Components/Layout/NavMenu.razor` (lien « Résultat » → `/resultat`)
- Modifier : `src/Piscine.DevHost/Program.cs` (DI : `IPushResultWatcher` → `ProgressFileWatcher` singleton)
- Tests : `tests/Piscine.App.Tests/ProgressFileWatcherTests.cs` (écrire `progress.json` → événement + delta ; debounce ; pas de faux positif ; dispose) ; réutilise `TempDir.cs`
- Test : `tests/Piscine.Components.Tests/PushResultPanelTests.cs` (bUnit : vide / À revoir + lien / Réussi / auto-rafraîchi)
- Test : `tests/Piscine.DevHost.E2E/PushResultSmokeTests.cs` (Playwright : `progress.json` écrit → verdict rendu **sans clic**)
- **NE PAS toucher** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`, `.github/workflows/release.yml`.

---

### Task 1 : `PushResultWatcher` (surveille `progress.json` → événement + `LatestResult`) + tests unitaires

**Files:**
- Create: `src/Piscine.App/Push/PushResult.cs`, `IPushResultWatcher.cs`, `ProgressFileWatcher.cs`
- Test: `tests/Piscine.App.Tests/ProgressFileWatcherTests.cs` ; réutilise `TempDir.cs` (S3)

- [ ] **Step 0 — Vérifier l'API moteur** : relire `src/Piscine.Core/Progression/ProgressStore.cs`
  (`Load`/`Save`, `ProgressPath`), `Model/Progress.cs` (`Exercises`, `ExerciseProgress { Status, Attempts, LastAttempt }`),
  `Model/ExerciseStatus.cs`, `PiscineLayout.cs` (`StateDir`, `ProgressPath`). Confirmer avant de coder.

- [ ] **Step 1 — Modèles purs** `PushResult.cs` :
  - enum `PushVerdict { Reussi, ARevoir }` (mappé depuis `ExerciseStatus`).
  - `record PushResultEntry(string ExerciseId, PushVerdict Verdict, int Attempts, DateTimeOffset? LastAttempt)`.
  - `record PushResult(IReadOnlyList<PushResultEntry> Changed, DateTimeOffset ObservedAt)`.
  - enum `PushPhase { Idle, EnAttente, Recu }`. Aucune note/score.

- [ ] **Step 2 — Interface** `IPushResultWatcher.cs` : `IAsyncDisposable` + `event Action<PushResult>? ResultReceived`,
  `PushResult? LatestResult()`, `void Start()`. (Calque `ICoachingChannel`.)

- [ ] **Step 3 — Impl** `ProgressFileWatcher.cs`. Ctor `(PiscineLayout layout)`. **LIT seulement.**
  - `Start()` (synchrone, idempotent) : `Directory.CreateDirectory(_layout.StateDir)` ; snapshot initial
    `_last = new ProgressStore(_layout.ProgressPath).Load()` (sans publier) ; créer
    `FileSystemWatcher(_layout.StateDir){ Filter="progress.json", NotifyFilter=LastWrite|FileName, EnableRaisingEvents=true }` ;
    s'abonner à `Created`+`Changed`+`Renamed` → `OnChanged`.
  - **Debounce** : `OnChanged` (re)arme un `Timer` à 250 ms → `Settle()`.
  - `Settle()` : `current = ProgressStore.Load()` ; delta vs `_last` (Status ou Attempts différent, ou nouveau) →
    `List<PushResultEntry>`. Si non vide : `_latest = new PushResult(delta, DateTimeOffset.Now); _last = current; ResultReceived?.Invoke(_latest);`.
    Try/catch `IOException`/`JsonException` (lecture pendant écriture) → réarmer le debounce une fois.
  - `LatestResult()` → `_latest` (thread-safe : `lock`/volatile).
  - `DisposeAsync` : `EnableRaisingEvents=false`, désabonner, dispose watcher + timer.

- [ ] **Step 4 — Tests unitaires** `ProgressFileWatcherTests.cs` (réutilise `TempDir`). `state = <temp>/.state` ;
  `layout = new PiscineLayout(content, <temp>/workspace, state)` (adapter au vrai ctor) ; helper `WriteProgress`.
  - **Détection** : `Start()` (state vide) ; s'abonner ; écrire `ex00-hello=ARevoir,1` → attendre l'événement
    (TaskCompletionSource + timeout ~3 s) → `Changed` contient `ex00-hello`, `Verdict==ARevoir`, `Attempts==1` ;
    `LatestResult()` égal.
  - **Delta seul** : pré-écrire `ex00-hello=Reussi` puis `Start()` ; écrire `ex01-foo=ARevoir` → événement ne
    contient **que** `ex01-foo`.
  - **Pas de faux positif** : ré-écrire le même contenu → aucun événement.
  - **Debounce** : 5 `Save` rapprochés → **un seul** événement (dernier état).
  - **Réussi** : `Reussi,2` → `Verdict==Reussi`, `Attempts==2`, `LastAttempt` non null.
  - **Dispose** : après `DisposeAsync`, une écriture ne déclenche plus ; nettoyage `TempDir` sans `IOException`.

- [ ] **Step 5 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning ; `ProgressFileWatcherTests` PASS ; tests App existants (S2–S7) verts.

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): PushResultWatcher (surveille progress.json ecrit par grade-received, debounce + snapshot-diff, lecture seule) + tests"
```

### Task 2 : composant `PushResultPanel` (verdict de statut + lien /check) + bUnit

**Files:**
- Create: `src/Piscine.Components/Components/Push/PushResultPanel.razor` (+ `.razor.css`)
- Test: `tests/Piscine.Components.Tests/PushResultPanelTests.cs`

- [ ] **Step 1 — Le composant** `PushResultPanel.razor` : `@page "/resultat"`, `@rendermode InteractiveServer`,
  `@namespace Piscine.Components.Components.Push`, `@using Piscine.App.Push`, `@using Piscine.App.Progress`,
  `@inject IPushResultWatcher Watcher`, `@implements IAsyncDisposable`. Calque l'abonnement de `/terminal` (S3).
  - `OnInitialized` : `Watcher.Start(); Watcher.ResultReceived += OnResult; _result = Watcher.LatestResult();`.
  - `OnResult(PushResult r)` → `await InvokeAsync(() => { _result = r; _phase = PushPhase.Recu; StateHasChanged(); });` (try/catch circuit fermé).
  - **Phase** : `data-testid="push-phase"` (« En attente du rendu… » / « Résultat reçu »).
  - **Placeholder** (`_result is null`) : `data-testid="push-empty"` « Pousse ton travail (`git push`) ; le verdict s'affichera ici automatiquement. »
  - **Par exercice** : `data-testid="push-entry"` →
    `<StatusBadge Status="MapStatus(e.Verdict)" Source="StatusSource.Progress" />` (`ARevoir → ExerciseProgressStatus.ARevoir` ; `Reussi → ExerciseProgressStatus.PousseNote`), l'exo, `LastAttempt`, `Attempts`.
  - **Lien diff riche** : pour chaque entrée `ARevoir`, `data-testid="push-check-link"` `href="/check"` « Voir le détail (diff attendu/obtenu) ».
  - `DisposeAsync` : `Watcher.ResultReceived -= OnResult;` (ne PAS disposer le watcher — singleton de l'hôte).

- [ ] **Step 2 — CSS scopé** `PushResultPanel.razor.css` (auto-bundlé, pas de `@Assets`).

- [ ] **Step 3 — bUnit** `PushResultPanelTests.cs` (`BunitContext`, `Render<T>`). Injecter un **faux**
  `IPushResultWatcher` (impl de test : `LatestResult()` configurable, `ResultReceived` déclenchable, `Start`/`DisposeAsync` no-op) :
  - **Vide** : `LatestResult()=null` → `push-empty` présent ; aucun `push-entry`.
  - **À revoir** : `PushResult([new("ex00-hello", ARevoir, 1, now)], now)` → `push-entry` ; un `status-badge`
    `data-status="ARevoir"` ; `push-check-link` `href="/check"`.
  - **Réussi** : entrée `Reussi` → `status-badge` `data-status="PousseNote"` ; **aucun** `push-check-link`.
  - **Auto-rafraîchi** : `LatestResult()=null` au rendu ; déclencher `ResultReceived` → `cut.WaitForState(...)` →
    `push-entry` apparaît **sans interaction**.

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: 0 warning ; `PushResultPanelTests` PASS ; bUnit existants (S4–S7) verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): composant PushResultPanel (verdict de statut via StatusBadge + lien /check, auto-rafraichi) + bUnit"
```

### Task 3 : page `/resultat` câblée (DI DevHost) + lien NavMenu

**Files:**
- Modify: `src/Piscine.DevHost/Program.cs`, `src/Piscine.Components/Components/Layout/NavMenu.razor`

- [ ] **Step 1 — DI dans le DevHost** `Program.cs` :
  ```csharp
  builder.Services.AddSingleton<Piscine.App.Push.IPushResultWatcher>(sp =>
      new Piscine.App.Push.ProgressFileWatcher(sp.GetRequiredService<PiscineLayout>()));
  ```
  Ajouter les `using`. Routage RCL déjà câblé (`AddAdditionalAssemblies(...MarkdownView.Assembly)`).

- [ ] **Step 2 — Lien NavMenu** : « Résultat » → `/resultat`, `data-testid="nav-resultat"`, logique `active` comme les autres.

- [ ] **Step 3 — Vérif visuelle (preview)** : `dotnet run --project src/Piscine.DevHost --urls http://localhost:5252`
  avec `PISCINE_CONTENT` vers `content/` **et** `PISCINE_HOME` vers un dossier temp jetable. `preview_start` sur
  `/resultat` → `push-empty` ; écrire un `progress.json` sous `<home>/.state` → la page s'auto-rafraîchit,
  `push-entry` apparaît **sans clic**. `preview_screenshot`.

- [ ] **Step 4 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: 0 warning ; `/resultat` routable, lien NavMenu présent.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): page /resultat cablee (DI DevHost ProgressFileWatcher via PiscineLayout) + lien NavMenu"
```

### Task 4 : E2E Playwright (artefact `progress.json` écrit → verdict rendu sans action manuelle)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/PushResultSmokeTests.cs`

- [ ] **Step 1 — Le test** : calquer `ProgressSmokeTests`/`InitSmokeTests` (port **5261**, poll, **skip propre
  sans Chromium**, racine via `Piscine.slnx`, kill arbre + `Directory.Delete` en `DisposeAsync`).
  - **Poste isolé** : `_tempHome = <temp>/piscine-e2e-resultat-<guid>`, `_tempWorkspace = <home>/workspace`,
    `stateDir = <home>/.state` (créer state+workspace, **NE PAS** pré-écrire `progress.json`). Env `dotnet run`
    (`WorkingDirectory = repoRoot`) : `PISCINE_CONTENT=<repo>/content`, `PISCINE_HOME=_tempHome`, `PISCINE_WORKSPACE=_tempWorkspace`.
  - **Parcours « sans action manuelle »** : (1) `GotoAsync("/resultat")` ; attendre `[data-testid='push-empty']`.
    (2) Écrire `progress.json` via l'API moteur :
    ```csharp
    var progress = new Progress();
    progress.Exercises["ex00-hello"] = new ExerciseProgress { Status = ExerciseStatus.ARevoir, Attempts = 1, LastAttempt = DateTimeOffset.Now };
    new ProgressStore(Path.Combine(stateDir, "progress.json")).Save(progress);
    ```
    (3) **Sans cliquer**, attendre `[data-testid='push-entry']` → asserter un `[data-testid='status-badge'][data-status='ARevoir']`
    + un `[data-testid='push-check-link']` `href="/check"`. (Timeout généreux : debounce 250 ms + circuit.)
  - `tests/Piscine.DevHost.E2E` référence déjà `Piscine.Core` (S5) — pas de nouvelle réf.

- [ ] **Step 2 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium en CI).

- [ ] **Step 3 — Commit**

```bash
git add -A
git commit -m "test(v4): E2E /resultat (progress.json ecrit -> verdict rendu sans action manuelle)"
```

### Task 5 : vérification globale + garde-fous + PR + issue de suivi

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** ; tous verts (235 S7 + `ProgressFileWatcherTests` + `PushResultPanelTests` ;
E2E se sautent sans Chromium).

- [ ] **Step 2 — Garde-fous (grader headless intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. `PushResultWatcher` **lit** `progress.json` via `ProgressStore` ; ne modifie ni
`GradeReceivedCommand`, ni `ProgressRecorder`, ni le hook. `grade-received` tourne **inchangé**.

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR** (commit et push en **appels séparés**)

```bash
git push -u origin v4/s8-push-surveillance
gh pr create --base main --title "v4 S8 — surveillance du resultat de push (grade-received)" --body-file <fichier>
```

- [ ] **Step 5 — Suivi : seam résultat riche.** Ouvrir une **issue de suivi** (label `v4`) : « `grade-received`
  persiste aussi le résultat riche (`last-push-result.json`) pour rendre le diff post-push sans re-jouer ».
  Préciser : **modif comportementale du grader headless** (hors « inchangé ») ; seam minimal = sérialiser
  `ExerciseGradingResult` + hint + `course_ref` à côté de `progress.json` ; test = artefact lu → `CheckFeedback`
  (S4) câblé sur le watcher. **Ne pas l'implémenter ici.**

---

## Self-review (couverture S8 vs issue #29 / spec §6)

- **Objectif** « après un `git push`, afficher richement le verdict » → `PushResultWatcher` (T1) publie le
  verdict, `PushResultPanel` (T2) l'affiche **sans action manuelle** (T2/T4). ✅ — **limite honnête** : seul le
  **verdict de statut** est persisté ; le diff riche est offert via un lien `/check` (S4) qui le re-calcule.
- **Périmètre** : surveillance de l'artefact `grade-received` (**headless inchangé**) → watcher
  `FileSystemWatcher` sur `progress.json`, **lecture seule** ✅ T1 · rendu réutilisant un composant existant →
  **`StatusBadge` (S5)** (pas `CheckFeedback` S4, infaisable post-hoc) ✅ T2 · signal « rendu en cours / terminé »
  → `PushPhase` ✅ T2.
- **Critères d'acceptation** : « un push déclenche l'affichage **sans action manuelle** » → abonnement
  `ResultReceived` + `InvokeAsync`/`StateHasChanged` ✅ T2 ; prouvé **bUnit** (auto-rafraîchi) ✅ T2 **et E2E**
  (artefact écrit → `push-entry` rendu sans clic) ✅ T4.
- **Dépendances** : S1 ✅ ; S4 (rendu réutilisé via `/check`) ✅ ; S5 (`StatusBadge`/`ExerciseProgressStatus`)
  réutilisé ✅ ; S3 (FORMAT canal/événement) calqué ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors 0 warning / NU1605 surveillés (T5) · moteur + `Cli` + `release.yml`
  **INTACTS** ; **grader headless inchangé** (gate diff vide T5) · `@rendermode InteractiveServer` (T2) · routage
  RCL déjà câblé (S1) · CSS scopé auto-bundlé, pas de `@Assets` (T2) · bUnit 2.x (T2) · Playwright
  skip-sans-Chromium + racine `Piscine.slnx` + parallélisation désactivée + **port 5261** (T4) · env
  `PISCINE_HOME`/`PISCINE_WORKSPACE` isolés (T4) · `FileSystemWatcher` debounce + relecture + snapshot-diff +
  `InvokeAsync` + dispose (T1/T2) · `ClearReadOnly`/`Directory.Delete` au nettoyage E2E (T4).
- **Risque principal maîtrisé** : `grade-received` ne persiste que `progress.json` → statut-only + pont `/check` ;
  **seam résultat riche signalé en suivi** (T5 Step 5), **non** glissé dans S8.
- **Déterminisme/isolation** : watcher lit `progress.json` (vérité) via `ProgressStore` ; tests écrivent via
  `ProgressStore.Save` (format garanti) ; `TempDir`/`PISCINE_HOME` jetables ; debounce coalesce ; snapshot-diff
  neutralise événements ratés/dupliqués.
