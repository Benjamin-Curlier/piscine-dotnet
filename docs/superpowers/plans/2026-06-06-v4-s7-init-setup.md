# v4 Sprint 7 — Init/setup in-app (enrobe `piscine init`) — Plan d'implémentation

> Issue #28 (milestone « v4 — application desktop Photino »). Branche : `v4/s7-init-setup`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md)
> §5 (« Pas d'`origin` / `init` non fait → bouton Init »), §6 (rituel git/hook préservé à l'identique).
> Plans précédents : [S4](2026-06-06-v4-s4-check-feedback.md) (FORMAT canonique : wrap-a-command),
> [S5](2026-06-06-v4-s5-navigation-progression.md) (E2E à état isolé par env).

> **For agentic workers:** REQUIRED SUB-SKILL : superpowers:subagent-driven-development ou
> superpowers:executing-plans, tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR
> (le parent ajoute le trailer `Co-Authored-By`). **Aucune nouvelle dépendance** : on réutilise
> `Piscine.Git` (déjà référencé par `Piscine.App` — `GitWorkspace`, `HookScript`), LibGit2Sharp
> (transitif), bUnit 2.7.2 + Playwright (S1). Le moteur n'est PAS modifié — `InitService` l'APPELLE.

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Point d'entrée moteur** : `InitService` appelle **directement** `GitWorkspace.Initialize(layout, exe)`
  (`src/Piscine.Git/GitWorkspace.cs`) — le **même appel** que la commande CLI `init` (`src/Piscine.Cli/Program.cs`,
  wrapper qui résout `Environment.ProcessPath` puis appelle `GitWorkspace.Initialize`). `Piscine.App`
  **référence déjà** `Piscine.Git` et `GitWorkspace`/`HookScript` sont `public static`. **→ AUCUN seam moteur
  nécessaire.** On ne touche ni `Piscine.Cli`, ni `Piscine.Git`, ni `Piscine.Core`.
- **(b) Idempotence = déjà garantie par le moteur.** `GitWorkspace.Initialize` garde **chaque** étape
  (`if (!Repository.IsValid(...))`, `if (Remotes[name] is null)`, `CreateDirectory` no-op, hook réécrit à
  l'identique). Test moteur existant `GitWorkspaceTests.Initialize_IsIdempotent` (vert). `InitService.Initialize()`
  n'a donc PAS besoin de re-garder ; il **snapshote le statut avant/après** pour rapporter honnêtement
  « déjà initialisé » vs « créé ».
- **(c) Détection « déjà initialisé »** (lecture seule) : `Repository.IsValid(layout.WorkspaceRoot)` &&
  `Repository.IsValid(layout.RemoteRepoPath)` && `File.Exists(<bare>/hooks/post-receive)` (+ option : remote
  `origin` présent). `IsInitialized` = ces trois vrais.
- **(d) Chemin de l'exécutable du hook** (la **seule** vraie décision). Le CLI passe `Environment.ProcessPath`
  (= `piscine` packagé) MAIS sous nos hôtes Blazor le process est `dotnet`/`Piscine.DevHost`/`Piscine.Desktop` —
  un hook pointant le mauvais exe casserait `grade-received` au push. → `InitService` prend le **chemin de l'exe
  piscine en paramètre explicite** (ctor/option), surclassable par env `PISCINE_EXE`, défaut sinon. Les tests
  **assertent le contenu du hook contre le chemin qu'on a passé** (jamais `Environment.ProcessPath`).
- **(e) Top 3 risques** : 1) **chemin exe du hook** (cf. (d)) → param explicite + env + test ciblé ;
  2) **moteur intact** sous WarningsAsErrors (gate diff vide T5, NU1605 — aucune dep nouvelle) ;
  3) **déterminisme/isolation** : init **écrit** sur disque → `TempDir` + `PiscineLayout` explicite (unit) ;
  `PISCINE_HOME`/`PISCINE_WORKSPACE` temp jetable (E2E) ; nettoyage `ClearReadOnly` (git read-only Windows) ;
  fermer chaque `Repository` (`using`).

**Goal:** Permettre à la recrue d'**initialiser son environnement git depuis l'app, sans terminal** : un
bouton **Init** qui enrobe la commande `init` existante (workspace + dépôt **bare local** « origin » + hook
**`post-receive`** qui lance `grade-received`). Le bouton affiche l'état courant (« déjà initialisé » vs
« non initialisé ») et le résultat du clic ; il est **idempotent**. Prouvé par xUnit (vierge→initialisé,
re-run idempotent, détection statut, contenu du hook) + bUnit (deux états + après-clic) + E2E Playwright
(poste vierge temp → clic Init → initialisé ; re-clic → idempotent).

**Architecture:** Logique dans **`Piscine.App`** (sans UI ni Photino), consommée à l'identique par Photino et
le harnais DevHost (spec §4). `InitService` est un **wrapper mince** : il APPELLE `GitWorkspace.Initialize`
(déjà référencé) — il ne duplique ni ne modifie aucune logique git/init. Le rendu (`InitPanel.razor`,
`@page "/init"`, `@rendermode InteractiveServer`) vit dans la RCL `Piscine.Components`, câblé au NavMenu.
Moteur (`Core`/`Grading`/`Git`), `Piscine.Cli` et `release.yml` **INTACTS**.

**Tech Stack:** .NET 10 ; `Piscine.Git` (réf. existante — `GitWorkspace`, `HookScript`) ; `Piscine.Core`
(`PiscineLayout` : `WorkspaceRoot`, `RemoteRepoPath`, `StateDir`) ; LibGit2Sharp (transitif, `Repository.IsValid`) ;
Blazor (RCL, `@rendermode InteractiveServer`) ; xUnit 2.9.3 ; bUnit 2.7.2 (`BunitContext`/`Render<T>`) ;
Playwright (skip-sans-Chromium, racine via `Piscine.slnx`, parallélisation désactivée, port dédié).
**Aucune nouvelle référence NuGet.**

---

## ⚠️ Note de risque

**Risque principal** : le **chemin de l'exécutable inscrit dans le hook `post-receive`**. Le CLI utilise
`Environment.ProcessPath` (= `piscine` packagé) ; sous le DevHost/Photino le process est `dotnet`/un hôte
Blazor. Un hook pointant le mauvais binaire **casserait `grade-received` au push** (rituel git, spec §6).
→ `InitService` prend le **chemin du binaire piscine en paramètre explicite** (surclassable par `PISCINE_EXE`),
et le **test unitaire vérifie que le hook contient le chemin fourni** (assertion sur notre entrée, pas sur
`Environment.ProcessPath` du runner). Le contenu du hook n'est **jamais réécrit à la main** : délégué à
`HookScript.PostReceive(exe)` (déjà cross-platform : slashes pour MinGit, LF), comme le CLI.

**Risque secondaire** : init **écrit** sur disque (contraste avec check/progress, lecture seule). Isolation :
unit = `TempDir` + `PiscineLayout` explicite (jamais `~/piscine` réel) ; E2E = `PISCINE_HOME`/`PISCINE_WORKSPACE`
vers un dossier temp **vierge** jetable, nettoyé en `DisposeAsync` avec `ClearReadOnly` (git marque ses objets
read-only sous Windows → `Directory.Delete` échoue sinon ; déjà géré par `TempDir.cs`). Fermer chaque
`Repository` (`using`).

**Idempotence** : déjà assurée par le moteur. `InitService.Initialize()` appelle sans re-garder ; snapshote
`Status()` avant/après pour distinguer « créé » de « déjà présent ».

## Carte des fichiers

- Créer : `src/Piscine.App/Init/InitStatus.cs` (record statut + `IsInitialized` ; PUR)
- Créer : `src/Piscine.App/Init/InitOutcome.cs` (record résultat : succès, avant/après, message, erreur ; PUR)
- Créer : `src/Piscine.App/Init/InitService.cs` (`Status()` lecture seule ; `Initialize()` → `GitWorkspace.Initialize` → `InitOutcome` ; chemin exe explicite)
- Créer : `src/Piscine.Components/Components/Init/InitPanel.razor` (`@page "/init"`, `@rendermode InteractiveServer`) + `.razor.css`
- Modifier : `src/Piscine.Components/Components/Layout/NavMenu.razor` (lien « Initialiser » → `/init`)
- Modifier : `src/Piscine.DevHost/Program.cs` (DI : `InitService` depuis le `PiscineLayout` déjà enregistré + `PISCINE_EXE`)
- Tests : `tests/Piscine.App.Tests/InitServiceTests.cs` (vierge→initialisé ; re-run idempotent ; statut ; hook=chemin fourni) ; réutilise `TempDir.cs`
- Test : `tests/Piscine.Components.Tests/InitPanelTests.cs` (bUnit : non-init / déjà-init / après-clic)
- Test : `tests/Piscine.DevHost.E2E/InitSmokeTests.cs` (Playwright : vierge → Init → initialisé ; re-clic idempotent)
- **NE PAS toucher** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`, `.github/workflows/release.yml`.

---

### Task 1 : `InitService` + modèles statut/résultat (wrappe `GitWorkspace`) + tests unitaires

**Files:**
- Create: `src/Piscine.App/Init/InitStatus.cs`, `InitOutcome.cs`, `InitService.cs`
- Test: `tests/Piscine.App.Tests/InitServiceTests.cs` ; réutilise `tests/Piscine.App.Tests/TempDir.cs`

- [ ] **Step 0 — Vérifier l'API moteur** : relire `src/Piscine.Git/GitWorkspace.cs` (`Initialize`, `OriginName`),
  `src/Piscine.Git/HookScript.cs` (`PostReceive`), `src/Piscine.Core/PiscineLayout.cs` (`WorkspaceRoot`,
  `RemoteRepoPath`, `StateDir`). Confirmer les signatures avant de coder.

- [ ] **Step 1 — Modèles purs** :
  - `InitStatus.cs` : `record InitStatus(bool WorkspaceReady, bool BareRepoReady, bool HookInstalled, bool OriginConfigured)`
    + `bool IsInitialized => WorkspaceReady && BareRepoReady && HookInstalled;` (origin informatif).
  - `InitOutcome.cs` : `record InitOutcome(bool Success, InitStatus Before, InitStatus After, string Message, string? Error)`.

- [ ] **Step 2 — Le service** `InitService.cs`. **Wrapper mince**. Ctor `(PiscineLayout layout, string piscineExecutablePath)`.
  - `InitStatus Status()` — lecture seule : `workspace = Repository.IsValid(_layout.WorkspaceRoot)` ;
    `bare = Repository.IsValid(_layout.RemoteRepoPath)` ; `hook = File.Exists(Path.Combine(_layout.RemoteRepoPath, "hooks", "post-receive"))` ;
    `origin` : si `workspace`, `using var repo = new Repository(_layout.WorkspaceRoot)` → `repo.Network.Remotes[GitWorkspace.OriginName] is not null`, sinon false.
  - `InitOutcome Initialize()` : `before = Status()` ; `try { GitWorkspace.Initialize(_layout, _exe); } catch (Exception ex) when (ex is LibGit2SharpException or IOException or UnauthorizedAccessException) { return new(false, before, before, "Échec de l'initialisation.", ex.Message); }` ; `after = Status()` ; `msg = before.IsInitialized ? "Déjà initialisé." : "Environnement initialisé."` ; `return new(after.IsInitialized, before, after, msg, null)`.

- [ ] **Step 3 — Tests unitaires** `InitServiceTests.cs`. Par test : `TempDir` + `layout = new PiscineLayout(content, ws, state)` (adapter au vrai ctor) ; `const string Exe = "/usr/local/bin/piscine";`.
  - **Vierge → initialisé** : `Status().IsInitialized` false ; `r = Initialize()` → `Success` true, `Before.IsInitialized` false, `After.IsInitialized` true, `Message == "Environnement initialisé."`, `Error` null ; `Repository.IsValid` workspace+bare ; hook existe.
  - **Hook = chemin fourni** : `File.ReadAllText(<hook>)` contient `grade-received` ET le chemin `Exe`.
  - **Détection statut** : après init, `Status().IsInitialized` true (tous flags true).
  - **Re-run idempotent** : `Initialize()` ; `r2 = Initialize()` → `Success` true, `Before.IsInitialized` true, `Message == "Déjà initialisé."`, ne lève pas.
  - **(option) Hook supprimé → Status().IsInitialized false** (la détection inspecte bien les 3 signaux).

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning ; cas Init PASS ; tests App existants (S2–S5) verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): InitService (enrobe GitWorkspace.Initialize, idempotent) + statut/resultat + tests"
```

### Task 2 : composant `InitPanel` (statut + bouton Init + résultat) + bUnit

**Files:**
- Create: `src/Piscine.Components/Components/Init/InitPanel.razor` (+ `.razor.css`)
- Test: `tests/Piscine.Components.Tests/InitPanelTests.cs`

- [ ] **Step 1 — Le composant** `InitPanel.razor` : `@page "/init"`, `@rendermode InteractiveServer`,
  `@namespace Piscine.Components.Components.Init`, `@using Piscine.App.Init`, `@inject InitService Init`.
  Calque `CheckPage.razor` (async on click, `_running`, `Task.Run`).
  - **Statut** (`OnInitialized` : `_status = Init.Status();`) : `data-testid="init-status"`,
    `data-initialized="@(_status?.IsInitialized)"` → « Déjà initialisé » / « Non initialisé ».
  - **Bouton** `data-testid="run-init"` « Initialiser » → `@onclick="RunInit"`, `disabled="@_running"`.
  - **Résultat** : si `_outcome is not null`, `data-testid="init-result"`, `data-success="@_outcome.Success"`,
    `_outcome.Message` ; si `_outcome.Error is not null`, `data-testid="init-error"`.
  - `@code` : `RunInit` async → `_outcome = await Task.Run(() => Init.Initialize()); _status = _outcome.After;`
    (gardé par `_running`, `StateHasChanged` dans `finally`).
  - Optionnel : si initialisé, lien `<a href="/terminal">` « Ouvrir le terminal » (cohérent spec §5).

- [ ] **Step 2 — CSS scopé** `InitPanel.razor.css` (auto-bundlé, pas de `@Assets`).

- [ ] **Step 3 — bUnit** `InitPanelTests.cs` (`BunitContext`, `Render<T>`). `InitService` injecté sur un
  `PiscineLayout` pointant un `TempDir` (comportement réel, pas de mock) :
  - **Non initialisé** : layout vierge → `Services.AddSingleton(new InitService(layout, "/x/piscine"))` ;
    `Render<InitPanel>()` → `init-status` `data-initialized="False"` ; bouton `run-init` présent non-disabled.
  - **Déjà initialisé** : `GitWorkspace.Initialize(layout, "/x/piscine")` AVANT le rendu → `data-initialized="True"`.
  - **Après clic** : layout vierge, `cut.Find("[data-testid='run-init']").Click();` → `init-result` `data-success="True"`,
    message « Environnement initialisé » ; `init-status` repasse `data-initialized="True"`.
  - Nettoyer le `TempDir`.

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: 0 warning ; `InitPanelTests` PASS ; bUnit existants (S4–S6) verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): composant InitPanel (statut + bouton Init + resultat, idempotent) + bUnit"
```

### Task 3 : DI DevHost + lien NavMenu

**Files:**
- Modify: `src/Piscine.DevHost/Program.cs`, `src/Piscine.Components/Components/Layout/NavMenu.razor`

- [ ] **Step 1 — DI dans le DevHost** `Program.cs` : enregistrer `InitService` depuis le `PiscineLayout`
  déjà dans le conteneur (contrôlable par env). Exe du hook : env `PISCINE_EXE` sinon défaut `"piscine"` :
  ```csharp
  builder.Services.AddSingleton(sp =>
  {
      var layout = sp.GetRequiredService<PiscineLayout>();
      var exe = Environment.GetEnvironmentVariable("PISCINE_EXE") ?? "piscine";
      return new Piscine.App.Init.InitService(layout, exe);
  });
  ```

- [ ] **Step 2 — Lien NavMenu** : ajouter « Initialiser » → `/init`, `data-testid="nav-init"`, logique `active`
  comme « Progression »/« Vérifier ».

- [ ] **Step 3 — Vérif visuelle (preview)** : `dotnet run --project src/Piscine.DevHost --urls http://localhost:5252`
  avec `PISCINE_CONTENT` vers `content/` **et** `PISCINE_HOME` vers un dossier temp jetable (ne PAS toucher
  `~/piscine` réel). `preview_start` sur `/init` → « Non initialisé », cliquer → « Environnement initialisé » ;
  `preview_screenshot`.

- [ ] **Step 4 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: 0 warning ; `/init` routable, lien NavMenu présent.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): page /init cablee (DI DevHost via PiscineLayout/PISCINE_EXE) + lien NavMenu"
```

### Task 4 : E2E Playwright (poste vierge → Init → initialisé ; re-clic idempotent)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/InitSmokeTests.cs`

- [ ] **Step 1 — Le test** : calquer `ProgressSmokeTests` (port dédié **5259**, poll, skip propre sans
  Chromium, racine via `Piscine.slnx`, kill arbre + `ClearReadOnly`/`Directory.Delete` en `DisposeAsync`).
  - **Poste vierge** : `_tempHome = <temp>/piscine-e2e-init-<guid>`, `_tempWorkspace = <home>/workspace`
    (NE PAS pré-initialiser). Env du `dotnet run` (`WorkingDirectory = repoRoot`) : `PISCINE_CONTENT=<repo>/content`,
    `PISCINE_HOME=_tempHome`, `PISCINE_WORKSPACE=_tempWorkspace`, `PISCINE_EXE=piscine`.
  - **Parcours** : `GotoAsync("/init")` ; attendre `[data-testid='init-status']` ; vérifier `data-initialized="False"` ;
    cliquer `[data-testid='run-init']` ; attendre `[data-testid='init-result']` ; asserter `data-success="True"`
    ET pas de `[data-testid='init-error']` ; `init-status` passe `data-initialized="True"`.
  - **Idempotence** : re-cliquer ; attendre résultat ; asserter `data-success="True"` ET message « Déjà initialisé »
    ET toujours aucun `init-error`. (Option : asserter sur FS que `<_tempHome>/.state/remote.git/hooks/post-receive` existe.)

- [ ] **Step 2 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium en CI).

- [ ] **Step 3 — Commit**

```bash
git add -A
git commit -m "test(v4): E2E /init (poste vierge -> Init -> initialise ; re-clic idempotent)"
```

### Task 5 : vérification globale + garde-fous (moteur intact) + PR

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** ; tous verts (225 S6 + InitServiceTests + InitPanelTests ; E2E se sautent sans Chromium).

- [ ] **Step 2 — Garde-fous (moteur intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. `InitService` **référence** `Piscine.Git` (`GitWorkspace`/`HookScript`), il ne le modifie pas.

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR** (commit et push en **appels séparés**)

```bash
git push -u origin v4/s7-init-setup
gh pr create --base main --title "v4 S7 — init/setup in-app (enrobe piscine init)" --body-file <fichier>
```

---

## Self-review (couverture S7 vs issue #28 / spec §5/§6)

- **Objectif** « initialiser depuis l'app, sans terminal » → `InitService.Initialize()` (T1) appelé par le bouton `/init` (T2/T3). ✅
- **Périmètre** : bouton/flux Init **enrobant `init`** (workspace + bare + hook) → `InitService` appelle `GitWorkspace.Initialize` (même appel que le CLI) ✅ T1 ; détection « déjà initialisé » → `Status()` ✅ T1, rendue ✅ T2.
- **Critères d'acceptation** : poste vierge → Init met en place workspace + bare + hook ✅ T1 (unit) + T4 (E2E) ; **idempotent** (garanti moteur) prouvé unit + bUnit + E2E (re-clic) ✅.
- **Dépendances** : S1 ✅ ; `Piscine.Git` déjà référencé → **aucun seam moteur** ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors 0 warning (T5) · moteur + `Cli` + `release.yml` **INTACTS** (gate diff vide T5) · `@rendermode InteractiveServer` (T2) · routage RCL déjà câblé (S1) · CSS scopé auto-bundlé, pas de `@Assets` (T2) · bUnit 2.x (T2) · Playwright skip-sans-Chromium + racine `Piscine.slnx` + parallélisation désactivée + **port 5259** (T4) · env `PISCINE_HOME`/`PISCINE_WORKSPACE` poste vierge temp (T4) · `ClearReadOnly` au nettoyage (T4).
- **Risque principal maîtrisé** : chemin exe du hook = **paramètre explicite** (`PISCINE_EXE`/ctor), jamais `Environment.ProcessPath` sous l'hôte Blazor ; contenu délégué à `HookScript.PostReceive` ; test asserte le hook contre le chemin fourni.
- **Déterminisme/isolation** : init **écrit** → `TempDir` + `PiscineLayout` explicite (unit/bUnit), `PISCINE_HOME`/`PISCINE_WORKSPACE` temp jetable (E2E), `Repository` fermés.
- **Suivi noté (S9)** : l'hôte Photino packagé devra fournir le **vrai** chemin du binaire `piscine` à `InitService` (pas `"piscine"` du PATH).
