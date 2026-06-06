# v4 Sprint 4 — `check` instantané in-process + rendu du feedback — Plan d'implémentation

> Issue #25 (milestone « v4 — application desktop Photino »). Branche : `v4/s4-check-feedback`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md) §6
> (« check instantané (boucle rapide) » : sélection exo → `CheckService` appelle `Piscine.Grading`
> **in-process** → rendu diff *attendu vs obtenu* + indices + `course_ref`, **sans git** ; remplace la
> sortie console de `piscine check`). Plans précédents : [S1](2026-06-06-v4-s1-foundation.md),
> [S2](2026-06-06-v4-s2-pty-spike.md), [S3](2026-06-06-v4-s3-git-coaching.md).

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development ou
> superpowers:executing-plans, tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR
> (le parent ajoute le trailer `Co-Authored-By`). **Aucune nouvelle dépendance** : on réutilise
> `Piscine.Grading` (déjà référencé par `Piscine.App`), `CourseCatalog`/`MarkdownRenderer` (RCL S1),
> bUnit 2.7.2 + Playwright (S1). Le moteur n'est PAS modifié — `CheckService` l'APPELLE.

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Point d'entrée moteur** : `CheckService` reproduit le chaînage de `CheckCommand.Run` **moins**
  la console et **moins** la persistance de progression : `ContentLocator.FindExercise` →
  `SubmissionLoader.Load(contentDir, workspaceDir)` → `ExerciseGrader.Grade(manifest, context)` (via
  `Graders.Default()`). Type retourné moteur = **`ExerciseGradingResult`** (`ExerciseId`, `Status`,
  `Results: IReadOnlyList<GraderResult>`). On **ne touche pas** `Piscine.Grading`.
  ⚠️ **À VÉRIFIER au début de T1** : les noms exacts (`ContentLocator.FindExercise`, `SubmissionLoader.Load`,
  `ExerciseGrader.Grade`, `Graders.Default()`, `PiscineLayout`, `ExerciseGradingResult`/`GraderResult`,
  `GraderStatus.{Reussi,ARevoir,NonCorrige}`) proviennent de la lecture du code ; lire `CheckCommand`/
  `ResultFormatter`/`IoGrader` et **adapter** si une signature diffère.
- **(b) DTO UI minimal** : le diff *attendu/obtenu* n'est PAS un champ structuré — `IoGrader` l'émet en
  **lignes de texte** dans `GraderResult.Messages` (`"Attendu : \"...\\n\""`, `"Obtenu  : \"...\\n\""`,
  retours à la ligne échappés en `\n` par son `Quote` privé). On définit donc un record UI
  `CheckOutcome` qui **réutilise `GraderResult` tel quel** (verdict + `Results` + `Trigger`) et ajoute
  l'**indice apparié** + le **`course_ref`** résolus depuis `manifest.Feedback` **exactement comme**
  `ResultFormatter.MatchHint` (sans dupliquer le moteur de règles : on lit `Trigger` → `Hints[].When`).
  Aucune note, aucun score (registre éducatif).
- **(c) Source gradable déterministe (tests)** : l'exo **réel** du repo
  `content/modules/00-setup-git/exercises/ex00-hello` (livrable `Hello.cs`, 1 cas `io` attendant
  `"Hello, Piscine!\n"`/exit 0 ; `feedback.hints[when=io_mismatch]` + `course_ref: cours.md#hello-world` ;
  `solution/Hello.cs` livrée). **Known-PASS** = copier `solution/Hello.cs` dans un workspace temp.
  **Known-FAIL+diff** = écrire un `Hello.cs` faux (`WriteLine("Bonjour")`) → `ARevoir`, `Trigger=io_mismatch`,
  messages `Attendu`/`Obtenu`, + l'indice du manifest. `PISCINE_CONTENT` = `content/` du repo.
  ⚠️ **À VÉRIFIER au début de T1** : que cet exo existe avec ce manifest exact ; sinon choisir un autre exo
  `io` du repo avec `solution/` + `feedback.hints` + `course_ref`.
- **(d) Top 3 risques** : 1) le diff est du **texte** (échappé) et non structuré → on rend `Messages`
  fidèlement et on **clé l'assertion sur le `Trigger`/`data-testid`**, pas sur des sous-chaînes
  fragiles ; 2) **moteur intact** sous WarningsAsErrors (gate diff vide en T5, surveiller NU1605) ;
  3) **résolution de contenu + isolation** → `PiscineLayout` explicite + `TempDir` par test (sinon
  `IsEmpty` au lieu d'un pass/fail).

**Goal:** Remplacer la sortie console de `piscine check` par un **feedback riche in-app**. Un service
pur `CheckService` (dans `Piscine.App`) localise un exercice, rassemble les livrables, **compile+exécute
in-process via `Piscine.Grading`** (sans git) et renvoie un **résultat structuré** (`CheckOutcome`, jamais
du texte console). Un composant RCL rend ce résultat : **verdict** global, **diff attendu vs obtenu** par
cas (espaces/retours à la ligne rendus visibles), **indices** (style des `HintCard` du coaching) et
**`course_ref`** en lien vers la page de cours. Un petit **sélecteur d'exercice** (liste depuis
`CourseCatalog`) déclenche le check et affiche le feedback dans une page DevHost `/check`. Prouvé par
bUnit (composant : pass et fail+diff) + E2E Playwright (sélection exo → feedback avec diff).

**Architecture:** Toute la logique vit dans **`Piscine.App`** (sans UI ni Photino) → consommée à
l'identique par Photino et le harnais DevHost (invariant spec §4). `CheckService` est **pur et
déterministe** : aucune dépendance terminal/git ; il APPELLE `Piscine.Grading` (déjà référencé par
`Piscine.App`) — il ne le modifie pas. Le composant de rendu (`CheckFeedback.razor`) + le sélecteur
(`CheckPage.razor`, `@page "/check"`, `@rendermode InteractiveServer`) vivent dans la RCL
`Piscine.Components` ; ils réutilisent `CourseCatalog` (S1) pour lister les exercices et le **style des
cartes d'indices** de `TerminalPage` (classes `hint-card hint-<severity>`). Moteur
(`Core`/`Grading`/`Git`), `Piscine.Cli` et `release.yml` **INTACTS**.

**Tech Stack:** .NET 10 ; `Piscine.Grading` (réf. existante de `Piscine.App` — `ExerciseGrader`,
`Graders.Default()`, `SubmissionLoader`, `ExerciseGradingResult`/`GraderResult`) ; `Piscine.Core`
(`ContentLocator`, `PiscineLayout`, `FeedbackConfig`) ; Blazor (RCL, `@rendermode InteractiveServer`) ;
`CourseCatalog`/`MarkdownRenderer` (RCL S1) ; xUnit 2.9.3 ; bUnit 2.7.2 (`BunitContext`/`Render<T>`) ;
Playwright (skip-sans-Chromium, racine via `Piscine.slnx`, parallélisation désactivée). Aucune nouvelle
référence NuGet.

---

## ⚠️ Note de risque (surface de l'API de notation → résultat structuré UI-friendly, sans toucher le moteur)

**Risque principal** : exposer un résultat **UI-friendly** à partir du type moteur **sans modifier
`Piscine.Grading`**. Le diff *attendu vs obtenu* n'existe pas en champ structuré : `IoGrader` le rend en
**lignes de texte** dans `GraderResult.Messages` (avec retours à la ligne **échappés** en `\n` par son
`Quote` privé), et l'appariement indice/`course_ref` est calculé par `ResultFormatter` (console) à partir
de `GraderResult.Trigger` + `manifest.Feedback`. → On **n'altère pas** le moteur : `CheckService` renvoie
un record `CheckOutcome` qui **embarque `Results` (donc `Messages`) verbatim** + le `Trigger` du premier
échec + l'indice/`course_ref` résolus **comme `ResultFormatter.MatchHint`** (lecture seule du manifest).
Le composant rend `Messages` **fidèlement** (bloc préformaté, espaces visibles) et **clé ses assertions
sur le `Trigger` / un `data-testid` stable**, jamais sur des sous-chaînes fragiles susceptibles de bouger
si le wording du moteur change.

**Risque secondaire** : source gradable **déterministe** pour les tests. → exo réel `ex00-hello`
(`PISCINE_CONTENT` = `content/` du repo, résolu via `PiscineLayout`), recrue simulée dans un `TempDir`
par test : `solution/Hello.cs` = PASS, `Hello.cs` faux = FAIL+diff. Pas de git (check git-less) →
**ne PAS** mobiliser `GitFixtureBuilder`.

**Caveat exécution in-process** (déjà géré par le moteur, à ne pas réimplémenter) : `ProgramRunner`
exécute le binaire compilé dans un `AssemblyLoadContext` **collectible**, redirige `Console.SetOut`,
applique un **timeout 5 s** et `alc.Unload()`. `CheckService` se contente d'appeler `ExerciseGrader.Grade`.

## Carte des fichiers

- Créer : `src/Piscine.App/Checking/CheckOutcome.cs` (record UI : verdict + cas + diff(messages) + indice + course_ref ; PUR)
- Créer : `src/Piscine.App/Checking/CheckService.cs` (localise + charge + `ExerciseGrader.Grade` in-process → `CheckOutcome` ; PUR, sans git, sans progression)
- Créer : `src/Piscine.Components/Components/Check/CheckFeedback.razor` (rend un `CheckOutcome`) + `CheckFeedback.razor.css`
- Créer : `src/Piscine.Components/Components/Check/CheckPage.razor` (`@page "/check"`, `@rendermode InteractiveServer` : sélecteur + bouton « Vérifier » → `CheckFeedback`)
- Modifier : `src/Piscine.DevHost/Program.cs` (DI : `CheckService` + `PiscineLayout`/contenu)
- Tests : `tests/Piscine.App.Tests/CheckServiceTests.cs` (known-pass via solution + known-fail+diff) ; `tests/Piscine.App.Tests/TempDir.cs` réutilisé (existe, S3)
- Test : `tests/Piscine.Components.Tests/CheckFeedbackTests.cs` (bUnit : pass → verdict OK sans diff ; fail → diff + indice + lien course_ref)
- Test : `tests/Piscine.DevHost.E2E/CheckSmokeTests.cs` (Playwright : `/check` → sélectionner exo → « Vérifier » → feedback)
- **NE PAS toucher** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`,
  `.github/workflows/release.yml`.

---

### Task 1 : `CheckService` + `CheckOutcome` dans `Piscine.App` (wrappe le moteur) + tests unitaires

**Files:**
- Create: `src/Piscine.App/Checking/CheckOutcome.cs`, `src/Piscine.App/Checking/CheckService.cs`
- Test: `tests/Piscine.App.Tests/CheckServiceTests.cs` ; réutilise `tests/Piscine.App.Tests/TempDir.cs` (S3)

- [ ] **Step 0 — Vérifier l'API moteur** : lire `src/Piscine.Grading/CheckCommand.cs`,
  `ResultFormatter.cs`, `IoGrader.cs`, `src/Piscine.Core/Content/ContentLocator.cs` + `PiscineLayout`.
  Confirmer/ajuster les signatures de la décision (a) avant de coder.

- [ ] **Step 1 — DTO UI pur** `src/Piscine.App/Checking/CheckOutcome.cs`. Records immuables, **aucune
  note** :
  - `CheckCaseResult(string GraderType, bool Passed, IReadOnlyList<string> Messages)` — `Messages` =
    `GraderResult.Messages` **verbatim** (le diff `Attendu`/`Obtenu` y est déjà, échappé par le moteur).
  - `CheckOutcome(string ExerciseId, string ModuleId, CheckVerdict Verdict, IReadOnlyList<CheckCaseResult> Cases, string? Hint, string? CourseRef)`.
  - enum `CheckVerdict { Reussi, ARevoir, AucunFichier, Introuvable }` (mappé depuis `GraderStatus` +
    cas « introuvable »/« vide »).

- [ ] **Step 2 — Le service** `src/Piscine.App/Checking/CheckService.cs`. **Pur, déterministe, sans
  git, sans persistance** (contraste avec `CheckCommand` qui écrit la progression et formate la console).
  Ctor : `CheckService(PiscineLayout layout, ExerciseGrader grader)`. `Check(string exerciseId) : CheckOutcome` :
  1. `ContentLocator.FindExercise(_layout.Content, exerciseId)` ; `null` → `Verdict = Introuvable`.
  2. `submission = SubmissionLoader.Load(location.ContentDir, _layout.WorkspaceExerciseDir(location.ModuleId, exerciseId))`.
  3. `submission.IsEmpty` → `Verdict = AucunFichier`.
  4. `result = _grader.Grade(submission.Manifest, submission.Context)` (in-process, géré par le moteur).
  5. `result.Results` → `CheckCaseResult` (`Passed = r.Status == GraderStatus.Reussi`, `Messages = r.Messages`).
  6. **Indice + course_ref** (si `ARevoir`, comme `ResultFormatter.MatchHint`) :
     `trigger = result.Results.FirstOrDefault(r => r.Status == ARevoir && r.Trigger is not null)?.Trigger` ;
     `Hint = manifest.Feedback.Hints.FirstOrDefault(h => h.When == trigger)?.Message` ;
     `CourseRef = string.IsNullOrWhiteSpace(manifest.Feedback.CourseRef) ? null : manifest.Feedback.CourseRef`.
  7. `Verdict` depuis `result.Status`. **Ne PAS** écrire `progress.json` (boucle rapide, non officielle).

- [ ] **Step 3 — Tests unitaires** `tests/Piscine.App.Tests/CheckServiceTests.cs`. Racine repo via
  `Piscine.slnx` → `contentRoot = <repo>/content` ; par test un `TempDir` workspace ;
  `layout = new PiscineLayout(contentRoot, tempWorkspaceRoot, tempStateDir)` (adapter au vrai ctor). Le
  livrable est posé sous `layout.WorkspaceExerciseDir(moduleId, exerciseId)`.
  - **Known-PASS** : copier `.../ex00-hello/solution/Hello.cs` → `Check("ex00-hello")` : `Verdict == Reussi`,
    tous `Cases[].Passed`, `Hint == null`, `CourseRef == null`.
  - **Known-FAIL + diff** : `Hello.cs` faux (`System.Console.WriteLine("Bonjour");`) → `Verdict == ARevoir` ;
    un `CheckCaseResult` `GraderType == "io"`, `Passed == false`, `Messages` contient une ligne « Attendu »
    **et** une « Obtenu » ; `Hint` non vide (= manifest) ; `CourseRef == "cours.md#hello-world"`.
  - **Introuvable** : `Check("ex-inexistant")` → `Introuvable`.
  - **AucunFichier** : workspace vide → `AucunFichier`.
  - **Déterminisme** : deux `Check` sur le known-pass → mêmes résultats (ALC unload).

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning ; les 5 cas PASS.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): CheckService in-process (wrappe Piscine.Grading, sans git ni progression) + CheckOutcome + tests pass/fail"
```

### Task 2 : composant `CheckFeedback` (verdict + diff par cas + indices + course_ref) + bUnit

**Files:**
- Create: `src/Piscine.Components/Components/Check/CheckFeedback.razor` (+ `CheckFeedback.razor.css`)
- Test: `tests/Piscine.Components.Tests/CheckFeedbackTests.cs`

- [ ] **Step 1 — Le composant** `CheckFeedback.razor`. `[Parameter] public CheckOutcome? Outcome`.
  Rendu (éducatif, **jamais de note**) :
  - **Verdict** : `data-testid="check-verdict"` (« Réussi » / « À revoir » / « Aucun fichier rendu » /
    « Exercice introuvable »), classes par statut.
  - **Par cas** non réussi : bloc `data-testid="check-case"` listant `Messages`. Les lignes `Attendu`/
    `Obtenu` dans un **bloc préformaté** (`<pre class="diff-line">`, `white-space: pre-wrap`) pour rendre
    visibles espaces et `\n` (déjà échappés par le moteur — on n'altère pas). Tag des lignes via
    `data-testid="diff-expected"` / `data-testid="diff-actual"` (détection : préfixe « Attendu » /
    « Obtenu » — uniquement pour le tag, le texte reste intégral).
  - **Indice** : si `Outcome.Hint` non null, carte style coaching (`<article class="hint-card hint-info" data-testid="check-hint">`).
  - **`course_ref`** : si `Outcome.CourseRef` non null, lien `data-testid="check-course-ref"` →
    `href="/module/@Outcome.ModuleId#@AnchorOf(Outcome.CourseRef)"` (`AnchorOf` = fragment après `#`,
    ex. `cours.md#hello-world` → `hello-world` ; à défaut → `/module/@Outcome.ModuleId`).
  - `Outcome` null → placeholder « Sélectionne un exercice puis Vérifier ».

- [ ] **Step 2 — CSS scopé** `CheckFeedback.razor.css` : verdict (succès/échec), bloc diff (`pre-wrap`,
  mono), réutiliser le look `hint-card`. (Auto-bundlé : **pas** de clé `@Assets`.)

- [ ] **Step 3 — bUnit** `tests/Piscine.Components.Tests/CheckFeedbackTests.cs` (`BunitContext`,
  `Render<T>()`). `CheckOutcome` construits **à la main** (records) :
  - **Pass** : `Verdict=Reussi`, cases passés, `Hint=null`, `CourseRef=null` → `check-verdict` présent
    (« Réussi ») ; **aucun** `check-hint` ; **aucun** `diff-expected`.
  - **Fail + diff** : `CheckCaseResult("io", false, ["La sortie ne correspond pas.", "Attendu : \"Hello, Piscine!\\n\"", "Obtenu  : \"Bonjour\\n\""])`,
    `Hint="..."`, `CourseRef="cours.md#hello-world"`, `ModuleId="00-setup-git"` → `diff-expected` **et**
    `diff-actual` présents ; `check-hint` présent ; `check-course-ref` est un `<a>` dont le `href` contient
    `/module/00-setup-git` et l'ancre `hello-world`.

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: 0 warning ; `CheckFeedbackTests` PASS ; `MarkdownViewTests` (S1) toujours vert.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): composant CheckFeedback (verdict + diff attendu/obtenu visible + indice + lien course_ref) + bUnit"
```

### Task 3 : sélecteur d'exercice + page `/check` (CheckService → CheckFeedback) + DI

**Files:**
- Create: `src/Piscine.Components/Components/Check/CheckPage.razor` (`@page "/check"`)
- Modify: `src/Piscine.DevHost/Program.cs` (DI `CheckService` + contenu)

- [ ] **Step 1 — DI dans le DevHost** `src/Piscine.DevHost/Program.cs` : enregistrer un `PiscineLayout`
  (contenu = `CourseCatalog.ContentRoot` déjà résolu ; workspace/state via `PiscineLayout.FromEnvironment`
  ou un dossier dev dédié) et `CheckService` (singleton sans état) :
  `builder.Services.AddSingleton(sp => new CheckService(<layout>, Graders.Default()))`. Ajouter les
  `using` requis. Routage RCL déjà câblé par `AddAdditionalAssemblies(typeof(Piscine.Components.MarkdownView).Assembly)` (S1).

- [ ] **Step 2 — La page** `CheckPage.razor` : `@page "/check"`, `@rendermode InteractiveServer`,
  `@inject CourseCatalog Catalog`, `@inject Piscine.App.Checking.CheckService Check`. **Minimal** (la
  navigation/progression complète = S5 #26) :
  - `<select data-testid="exo-select">` listant
    `Catalog.Modules.SelectMany(m => m.Groups).SelectMany(g => g.Exercises)` (value=`e.Id`, label=`e.Title`) ;
  - bouton `data-testid="run-check"` « Vérifier » → `@onclick` : `_outcome = Check.Check(_selectedId); StateHasChanged();` ;
  - `<CheckFeedback Outcome="_outcome" />` dessous.
  Pas de garde dev-only nécessaire (pas de shell OS : lecture + grading in-process inoffensifs).

- [ ] **Step 3 — Vérif visuelle (preview)** : `dotnet run --project src/Piscine.DevHost --urls
  http://localhost:5252` (avec `PISCINE_CONTENT` vers `content/` — piège connu), `preview_start` sur
  `/check`, sélectionner `ex00-hello`, « Vérifier » → verdict « Aucun fichier rendu » attendu (workspace
  vide par défaut → prouve le câblage `CheckService`). `preview_screenshot` comme trace. (Pass/fail réel
  avec diff prouvé par bUnit T2 + E2E T4.)

- [ ] **Step 4 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: 0 warning ; `/check` routable.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): page /check (selecteur d'exercice -> CheckService -> CheckFeedback) + DI DevHost"
```

### Task 4 : E2E Playwright (sélectionner un exo → feedback avec diff apparaît)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/CheckSmokeTests.cs`

- [ ] **Step 1 — Le test** : réutiliser le squelette `CoachingSmokeTests` (démarrage DevHost via
  `dotnet run --urls`, **port dédié** 5253, poll, **skip propre** si Chromium absent, racine via
  `Piscine.slnx`, kill arbre en `DisposeAsync` ; parallélisation déjà désactivée).
  - **Préparer un FAIL déterministe** dans `InitializeAsync` : écrire un `Hello.cs` faux dans le
    **workspace** que le DevHost utilisera pour `ex00-hello` (même `PiscineLayout` que la DI ;
    `WorkingDirectory = repoRoot` au `dotnet run` + fixer `PISCINE_CONTENT` / le workspace via variables
    d'env du processus pour un chemin prévisible et isolé). Nettoyer en `DisposeAsync`.
  - **Parcours** : `GotoAsync("/check")`, attendre `[data-testid="exo-select"]`, `SelectOptionAsync`
    `ex00-hello`, cliquer `[data-testid="run-check"]`, attendre `[data-testid="check-verdict"]`, puis
    asserter `[data-testid="diff-expected"]` présent (`count > 0`).

- [ ] **Step 2 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium en CI).

- [ ] **Step 3 — Commit**

```bash
git add -A
git commit -m "test(v4): E2E /check (selection exo -> feedback avec diff attendu/obtenu)"
```

### Task 5 : vérification globale + garde-fous (moteur intact) + PR

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** ; tous verts (195 S3 + `CheckServiceTests` (5) + `CheckFeedbackTests` (2) ;
E2E se sautent proprement sans Chromium).

- [ ] **Step 2 — Garde-fous (moteur intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. `CheckService` **référence** `Piscine.Grading`, il ne le modifie pas.

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR** (commit et push en **appels séparés**)

```bash
git push -u origin v4/s4-check-feedback
gh pr create --base main --title "v4 S4 — check instantane in-process + rendu du feedback" --body-file <fichier>
```

---

## Self-review (couverture S4 vs issue #25 / spec §6)

- **Objectif** « remplacer la sortie console de `piscine check` par un feedback riche in-app » →
  `CheckService` (T1, structure, pas de console ni progression) + `CheckFeedback` (T2). ✅
- **Périmètre** : `CheckService` appelle `Piscine.Grading` **in-process, sans git** (réutilise
  `ContentLocator`/`SubmissionLoader`/`ExerciseGrader.Grade`) ✅ T1 · rendu **diff attendu vs obtenu +
  indices + course_ref** ✅ T2 · sélecteur minimal ✅ T3 (navigation complète = S5 #26).
- **Critères d'acceptation** : « sélectionner un exo → feedback (diff + indices) » ✅ T3+T4 · **bUnit**
  pass **et** fail+diff ✅ T2 · **E2E parcours** ✅ T4. **Aucune note** partout ✅.
- **Dépendances** : S1 (RCL, `CourseCatalog`, squelette App, pyramide, bi-hôte) — réutilisée ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors 0 warning / NU1605 surveillés (T5) · moteur + `Cli` +
  `release.yml` **INTACTS** (`CheckService` **référence** Grading ; gate diff vide T5) ·
  `@rendermode InteractiveServer` (T3) · routage RCL déjà câblé (S1) · CSS scopé auto-bundlé, pas de clé
  `@Assets` (T2) · bUnit 2.x (`BunitContext`/`Render<T>`) (T2) · Playwright skip-sans-Chromium + racine
  `Piscine.slnx` + parallélisation désactivée + port dédié (T4) · **check git-less** → `GitFixtureBuilder`
  **non** mobilisé · caveat in-process (ALC, `Console.SetOut`, timeout 5 s) **délégué au moteur** (T1) ·
  `PISCINE_CONTENT` vers `content/` en dev/tests.
- **Risque principal maîtrisé** : diff = **texte échappé** dans `Messages` → `CheckOutcome` embarque
  `Messages` verbatim + `Trigger` ; le composant rend fidèlement et **clé ses assertions sur
  `Trigger`/`data-testid`**, pas sur des sous-chaînes fragiles.
- **Déterminisme** : exo réel `ex00-hello` (PASS via `solution/`, FAIL via livrable faux), `TempDir`
  workspace par test, `PiscineLayout` explicite, double-appel stable (ALC unload).
