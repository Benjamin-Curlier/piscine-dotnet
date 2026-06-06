# v4 Sprint 3 — Moteur statut git + coaching (shim git + état dépôt) — Plan d'implémentation

> Issue #24 (milestone « v4 — application desktop Photino »). Branche : `v4/s3-git-coaching`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md) §5
> (tableau de coaching + shim — **source de vérité**) et §6 (flux : chaque commande git → événement
> shim + lecture d'état → MAJ panneau statut + cartes d'indices). Plans précédents :
> [S1](2026-06-06-v4-s1-foundation.md), [S2](2026-06-06-v4-s2-pty-spike.md).

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development ou
> superpowers:executing-plans, tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR
> (le parent ajoute le trailer `Co-Authored-By`). Versions de paquets : aucune nouvelle dépendance —
> on réutilise LibGit2Sharp (transitif via `Piscine.Git`/`Piscine.Grading`) et `GitFixtureBuilder`
> (déjà dans `Piscine.Grading`, livré en V3 #9).

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Canal = named pipe .NET** (`NamedPipeServerStream`/`NamedPipeClientStream`) : cross-platform sur
  .NET 10 (socket domaine Unix sous le capot sur Linux/macOS, vrais pipes Windows), pas de port/firewall,
  le parent (`Piscine.App`) possède le cycle de vie → nom de pipe unique par session passé au shim par
  variable d'env. **Repli = loopback TCP** (`127.0.0.1:0` éphémère) derrière une interface
  `ICoachingChannel` (transport interchangeable). Le shim écrit **une ligne JSON** puis se déconnecte ;
  fire-and-forget, ne bloque jamais git.
- **(b) Résolution du vrai git sans récursion** : `PtyService` résout le vrai git **une fois** au
  démarrage de session (recherche PATH excluant le dossier du shim) et le passe en `PISCINE_REAL_GIT` ;
  le shim exécute ce chemin absolu. Défense secondaire : si la variable manque, le shim parcourt `PATH`
  en s'excluant lui-même (`Environment.ProcessPath`). Aucun git → exit 127 + stderr (n'arrive pas en
  exploitation normale). Sous Windows le vrai git = **MinGit** (livré dans le zip).
- **(c) Top 3 risques** : 1) fiabilité named pipe + coût de démarrage du shim → timeout 150 ms puis
  abandon silencieux, git relayé quand même, coaching dégradé en « lecture d'état seule » ; 2) PATH/env
  cross-platform dans le PTY → helper de construction d'env unit-testé + checklist OS proprio ; 3)
  déterminisme `RepoState` vs timing → lecture sans cache **après** l'événement shim, règles testées sur
  `GitFixtureBuilder`.

**Goal:** Dériver, **de façon déterministe**, le statut git d'un exercice (`GitStatusService`,
LibGit2Sharp, lecture seule) et **coacher** sur les erreurs (`CoachingService`, le tableau spec §5),
en s'appuyant sur deux signaux robustes : un **shim `git`** en tête de PATH qui émet un événement
structuré (`argv`/`exitCode`/`cwd`) par canal local (**named pipe**) — **jamais de parsing de stdout** —
et l'inspection d'état du dépôt. Câbler la boucle de coaching à côté du terminal S2 dans le DevHost,
prouvée par un E2E Playwright qui déclenche un indice.

**Architecture:** Tout vit dans **`Piscine.App`** (sans UI) → consommé à l'identique par Photino et le
harnais DevHost. `GitStatusService` + `CoachingService` sont **purs** (entrée = état + dernier
événement git ; sortie = `RepoState` / cartes d'indices ordonnées). Le **shim** est un **NOUVEAU**
projet exécutable `src/Piscine.GitShim/` (hors release), placé en tête de `PATH` dans
`PtyOptions.Environment` par `PtyService` (S2). Le récepteur du canal (`ICoachingChannel` →
`NamedPipeCoachingChannel`) vit dans `Piscine.App`. La page `/terminal` (RCL, S2) reçoit l'événement →
lit l'état → calcule les indices → affiche un panneau statut + des cartes. Moteur
(`Core`/`Grading`/`Git`), `Piscine.Cli` et `release.yml` **INTACTS** (le shim est un projet neuf, exclu
de la release console). LibGit2Sharp est dispo dans `Piscine.App` par transitivité (réf.
`Piscine.Git`/`Piscine.Grading`) → l'utiliser directement ; **ne PAS modifier `Piscine.Git`**.

**Tech Stack:** .NET 10 ; LibGit2Sharp (transitif, déjà résolu) ; `GitFixtureBuilder`
(`Piscine.Grading`, réutilisé pour les fixtures de test) ; `System.IO.Pipes` (named pipe, BCL) ;
`System.Text.Json` (payload une ligne) ; Blazor (RCL, `@rendermode InteractiveServer`) ; xUnit 2.9.3 ;
Playwright 1.60.0 (skip-sans-Chromium). Aucune nouvelle référence NuGet.

---

## ⚠️ Note de risque (shim IPC + résolution du vrai git + cross-platform)

Trois risques, avec repli :

1. **Fiabilité du canal (named pipe) + coût de démarrage du shim.** Un shim managé lancé à chaque
   `git` ajoute un handshake de pipe qui peut échouer (app fermée, course au 1er appel, charge). →
   **Repli intégré** : timeout de connexion ~150 ms côté shim, puis abandon silencieux **tout en
   relayant git normalement** ; le coaching **dégrade en « lecture d'état seule, sans événement de
   commande »** (la boucle déclenche quand même `GitStatusService` sur un debounce). Le shim émet en
   *fire-and-forget* : il ne bloque **jamais** git, même si personne n'écoute.
2. **PATH/env cross-platform dans le PTY.** Le dossier du shim doit être **préfixé** à `PATH` dans
   `PtyOptions.Environment` (confirmé : `PtyOptions.Environment` est un `IDictionary<,>`) pour tout
   shell (cmd/pwsh/bash/zsh), sans casser le PATH existant ; l'exe shim doit s'appeler exactement
   `git`/`git.exe`. → helper de construction d'env **unit-testé** ; checklist smoke par OS (action
   proprio, comme S2). Sur Windows le vrai git = **MinGit** (livré dans le zip) → `PISCINE_REAL_GIT`
   pointe dessus.
3. **Récursion du shim sur lui-même.** Le shim ne doit jamais ré-exécuter le shim. → `PtyService`
   résout le vrai git **une fois** (recherche PATH excluant le dossier du shim) et le passe en
   `PISCINE_REAL_GIT` ; le shim exécute ce chemin absolu. Défense secondaire : si la variable manque,
   le shim parcourt `PATH` en **excluant son propre exécutable** (`Environment.ProcessPath`).

**Détermination « cross-platform validé »** (comme S2/#23) = (i) Windows prouvé bout-en-bout par
l'agent (tests unitaires + E2E Playwright) + (ii) checklist smoke par OS rédigée pour le proprio.

## Carte des fichiers

- Créer : `src/Piscine.App/Git/RepoState.cs` (record d'état pur)
- Créer : `src/Piscine.App/Git/GitStatusService.cs` (LibGit2Sharp, lecture seule, pur)
- Créer : `src/Piscine.App/Coaching/HintCard.cs` (carte d'indice : id, titre, message, sévérité)
- Créer : `src/Piscine.App/Coaching/GitCommandEvent.cs` (record : `Argv`, `ExitCode`, `Cwd`)
- Créer : `src/Piscine.App/Coaching/ExerciseExpectation.cs` (record : branche attendue, etc.)
- Créer : `src/Piscine.App/Coaching/CoachingService.cs` (règles spec §5, pur)
- Créer : `src/Piscine.App/Coaching/ICoachingChannel.cs`, `NamedPipeCoachingChannel.cs` (récepteur)
- Modifier : `src/Piscine.App/Terminal/PtyStartInfo.cs` (+ `Environment` injectable), `PtyService.cs`
  (prépend PATH shim + `PISCINE_REAL_GIT` + nom du pipe) ; ajouter un helper `GitPathResolver.cs`
- Créer : `src/Piscine.GitShim/Piscine.GitShim.csproj` + `Program.cs` (relais + émission named pipe)
- Modifier : `Piscine.slnx` (ajouter `Piscine.GitShim` au build, **hors release**)
- Modifier : `src/Piscine.Components/Components/Terminal/TerminalPage.razor` (boucle de coaching :
  panneau statut + cartes) + `TerminalPage.razor.css` ; `src/Piscine.DevHost/Program.cs` (DI des
  services App + canal)
- Tests : `tests/Piscine.App.Tests/GitStatusServiceTests.cs`, `CoachingServiceTests.cs`,
  `GitPathResolverTests.cs`, `GitShimRelayTests.cs` (intégration shim), `TempDir.cs` (helper local)
- Test : `tests/Piscine.DevHost.E2E/CoachingSmokeTests.cs` (Playwright : `git commit` rien stagé → carte)
- **NE PAS toucher** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`,
  `.github/workflows/release.yml`.

---

### Task 1 : `GitStatusService` + `RepoState` + tests unitaires (GitFixtureBuilder)

**Files:**
- Create: `src/Piscine.App/Git/RepoState.cs`, `src/Piscine.App/Git/GitStatusService.cs`
- Create: `tests/Piscine.App.Tests/TempDir.cs`, `tests/Piscine.App.Tests/GitStatusServiceTests.cs`
- Modify: `tests/Piscine.App.Tests/Piscine.App.Tests.csproj` (réf. `Piscine.Grading` pour `GitFixtureBuilder`)

- [ ] **Step 1 — Modèle d'état pur** `src/Piscine.App/Git/RepoState.cs` : record immuable avec
  `IsRepository`, `CurrentBranch` (null si détaché/sans commit), `IsDetachedHead`, `HasAnyCommit`,
  `StagedCount`, `UnstagedCount`, `UntrackedCount`, `HasOrigin`, `AheadOfOrigin`,
  `ConflictedFiles` (`IReadOnlyList<string>`), et `HasUncommittedWork` (dérivé des compteurs).

- [ ] **Step 2 — Le service** `src/Piscine.App/Git/GitStatusService.cs` (LibGit2Sharp, lecture seule,
  pur : aucune écriture, aucun cache). `Read(workingDirectory)` :
  - `!Repository.IsValid(dir)` → `RepoState { IsRepository = false }` ;
  - `repo.RetrieveStatus(IncludeUntracked = true)` → compteurs (Staged+Added+RemovedFromIndex / Modified+
    Missing / Untracked) ;
  - `repo.Info.IsHeadDetached`, `repo.Head.Tip is not null`, `repo.Network.Remotes["origin"]` ;
  - `CurrentBranch` = `repo.Head.FriendlyName` sauf détaché/sans commit ;
  - `AheadOfOrigin` = commits atteignables depuis HEAD non atteignables depuis `origin/<branche>`
    (`CommitFilter { IncludeReachableFrom = head.Tip, ExcludeReachableFrom = tracked.Tip }`) ; 0 si pas
    de pendant distant ;
  - `ConflictedFiles` = `repo.Index.Conflicts` d'abord (signal net) ; filet = scan textuel des fichiers
    suivis pour les trois marqueurs (`<<<<<<<`, `=======`, `>>>>>>>`) **en début de ligne** (réutiliser
    la détection de `GitGrader`).

- [ ] **Step 3 — Helper de test** `tests/Piscine.App.Tests/TempDir.cs` : copier le `TempDir` existant
  (`tests/Piscine.Git.Tests/TempDir.cs`) sous le namespace `Piscine.App.Tests` (chaque projet de test a
  sa copie — pattern du repo ; il lève l'attribut read-only des objets git avant suppression).

- [ ] **Step 4 — Réf. `Piscine.Grading`** dans `tests/Piscine.App.Tests/Piscine.App.Tests.csproj`
  (pour appeler `GitFixtureBuilder.Build`) :
  `<ProjectReference Include="..\..\src\Piscine.Grading\Piscine.Grading.csproj" />`.

- [ ] **Step 5 — Tests** `tests/Piscine.App.Tests/GitStatusServiceTests.cs` : un dépôt par cas via
  `GitFixtureBuilder.Build` + manipulations LibGit2Sharp pour les états « vivants ». Couvrir :
  - dossier non-git → `IsRepository=false` ;
  - branche courante correcte + `HasAnyCommit` ;
  - HEAD détaché (`Commands.Checkout` sur un sha) → `IsDetachedHead`, `CurrentBranch=null` ;
  - rien stagé / fichier stagé / modifié non stagé / non suivi → compteurs ;
  - pas de remote → `HasOrigin=false` ; remote + commit local en avance → `AheadOfOrigin>0` ;
  - fichier avec `<<<<<<< ======= >>>>>>>` en début de ligne → `ConflictedFiles` non vide ; doc parlant
    de conflit (marqueurs PAS en début de ligne) → `ConflictedFiles` vide (anti-faux-positif).

- [ ] **Step 6 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning (WarningsAsErrors) ; tous les `GitStatusServiceTests` PASS.

- [ ] **Step 7 — Commit**

```bash
git add -A
git commit -m "feat(v4): GitStatusService + RepoState (lecture seule LibGit2Sharp) + tests fixtures"
```

### Task 2 : `CoachingService` + le tableau de règles spec §5 + tests par règle

**Files:**
- Create: `src/Piscine.App/Coaching/HintCard.cs`, `GitCommandEvent.cs`, `ExerciseExpectation.cs`,
  `CoachingService.cs`
- Test: `tests/Piscine.App.Tests/CoachingServiceTests.cs`

- [ ] **Step 1 — Modèles d'entrée/sortie** (purs, immuables) :
  - `HintCard(string Id, string Title, string Message, HintSeverity Severity)` + enum
    `HintSeverity { Info, Warn, Block }` (carte éducative, **jamais une note**).
  - `GitCommandEvent(IReadOnlyList<string> Argv, int ExitCode, string Cwd)` avec `Subcommand` = premier
    mot non-option (ex. `commit`).
  - `ExerciseExpectation { string? ExpectedBranch; bool? GradeReceivedFailed }`.

- [ ] **Step 2 — Le service** `src/Piscine.App/Coaching/CoachingService.cs`. **Pur** : `Evaluate`
  prend `RepoState` + dernier `GitCommandEvent?` (null = « état seul ») + `ExerciseExpectation` et
  renvoie une **liste ordonnée** de `HintCard` (le plus bloquant d'abord). **Agnostique au shell** : ne
  lit que `argv`/`exitCode`/état. Ton pédagogique (registre du moteur, phrases d'action). Règles par
  priorité (= tableau §5) :
  1. **init/origin manquant** (`!IsRepository` ou `!HasOrigin`) → « Lance d'abord l'initialisation. » `Block`.
  2. **Marqueurs de conflit** (`ConflictedFiles` non vide) → « Conflit non résolu : `<<<<<<<` présent. » `Block`.
  3. **HEAD détaché** → « HEAD détaché — reviens sur une branche (`git switch <b>`). » `Warn`.
  4. **Mauvaise branche** (`ExpectedBranch` ≠ `CurrentBranch`) → « Tu es sur `X`, l'exo attend `Y` (`git switch Y`). » `Warn`.
  5. **`commit` rien stagé** (`Subcommand=="commit"` ET `StagedCount==0`) → « Rien d'indexé — `git add` d'abord. » `Warn`.
  6. **Typo** (`ExitCode!=0` ET sous-commande inconnue à distance Levenshtein 1 d'une connue) → « `git comit` ? `commit` ? » `Info`.
  7. **Commité non poussé** (`commit` réussi OU `AheadOfOrigin>0`) → « Rendu non officiel tant que pas `git push origin <b>`. » `Info`.
  8. **Poussé mais grade KO** (`GradeReceivedFailed==true`) → « Le rendu est arrivé mais la correction signale un souci. » `Warn`.
  Suggestion typo = `Known.FirstOrDefault(k => Levenshtein(k, input) == 1)` sur une liste de
  sous-commandes connues. Levenshtein implémenté localement (pur).

- [ ] **Step 3 — Tests par règle** `tests/Piscine.App.Tests/CoachingServiceTests.cs` : **un `[Fact]`
  par ligne du tableau** + « état propre poussé → 0 carte » + l'**ordre de priorité** (conflit avant
  commité-non-poussé). Les `RepoState` sont construits **à la main** (records) — `CoachingService` est
  pur, pas besoin de dépôt réel (fixtures réelles déjà couvertes en T1).

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: 0 warning ; tous les `CoachingServiceTests` PASS (1 par règle + ordre + état propre).

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): CoachingService (regles spec 5, pur, agnostique shell) + tests par regle"
```

### Task 3 : projet shim `git` (relais transparent + émission named pipe) + test d'intégration

**Files:**
- Create: `src/Piscine.GitShim/Piscine.GitShim.csproj`, `src/Piscine.GitShim/Program.cs`
- Create: `src/Piscine.App/Coaching/ICoachingChannel.cs`, `NamedPipeCoachingChannel.cs`
- Modify: `Piscine.slnx` (ajouter `Piscine.GitShim`)
- Test: `tests/Piscine.App.Tests/GitShimRelayTests.cs`

- [ ] **Step 1 — Le projet shim** `src/Piscine.GitShim/Piscine.GitShim.csproj` : exe console minimal,
  `OutputType=Exe`, net10.0, Nullable+ImplicitUsings, **`AssemblyName=git`** (l'exe doit s'appeler
  `git`/`git.exe` pour intercepter en tête de PATH), `RootNamespace=Piscine.GitShim`, `IsPackable=false`.
  **Hors release** (jamais dans `release.yml`). Pas de réf. au moteur.

- [ ] **Step 2 — Le relais** `src/Piscine.GitShim/Program.cs` :
  1. Résoudre le vrai git (`PISCINE_REAL_GIT` d'abord ; sinon recherche PATH s'excluant via
     `Environment.ProcessPath`). Aucun → stderr + exit 127.
  2. Relayer transparemment : `ProcessStartInfo(realGit) { UseShellExecute=false }`, recopier `args`
     dans `ArgumentList`, héritage stdin/stdout/stderr, `WaitForExitAsync`, retourner `proc.ExitCode`.
  3. Émettre l'événement *fire-and-forget* : si `PISCINE_COACH_PIPE` défini, `NamedPipeClientStream(".",
     pipe, Out)`, `Connect(150)`, écrire une ligne JSON `{ argv, exitCode, cwd }`. Catch
     `TimeoutException`/`IOException`/`UnauthorizedAccessException` → abandon silencieux (**git jamais
     altéré**). **Aucun parsing de stdout.**

- [ ] **Step 3 — Récepteur côté App** `ICoachingChannel.cs` (interface : `Endpoint`,
  `event Action<GitCommandEvent>? CommandReceived`, `Start()`, `IAsyncDisposable`) +
  `NamedPipeCoachingChannel.cs` : `Endpoint = $"piscine-coach-{Guid:N}"` ; `Start()` lance une boucle
  d'acceptation (`NamedPipeServerStream(Endpoint, In, MaxAllowedServerInstances, Byte, Asynchronous)`,
  `WaitForConnectionAsync`, lire **une ligne** JSON → `GitCommandEvent` → `CommandReceived?.Invoke`,
  ré-écoute). Catch `OperationCanceledException` (stop) / `IOException`/`JsonException` (ré-écoute).

- [ ] **Step 4 — `Piscine.slnx`** : ajouter `src/Piscine.GitShim/Piscine.GitShim.csproj` dans `/src/`
  (buildé par la CI, exclu de la release — comme `Piscine.DevHost`).

- [ ] **Step 5 — Test d'intégration** `tests/Piscine.App.Tests/GitShimRelayTests.cs` : démarre un
  `NamedPipeCoachingChannel`, lance l'exe shim avec `PISCINE_COACH_PIPE=<endpoint>` et `PISCINE_REAL_GIT`
  pointant sur un **faux git** déterministe (un `.cmd`/`.sh` qui `exit 3`), vérifie (a) le code de sortie
  relayé (3) et (b) l'événement reçu (`Subcommand=="status"`, `ExitCode==3`). **Skip propre** si l'exe
  shim n'est pas localisable (catch → return, run verte).

- [ ] **Step 6 — Build solution + test**

Run: `dotnet build Piscine.slnx -c Release` puis
`dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: build 0 warning ; relais + émission PASS (ou skip propre si shim non localisé).

- [ ] **Step 7 — Commit**

```bash
git add -A
git commit -m "feat(v4): shim git (relais transparent + emission named pipe) + recepteur App + test integration"
```

### Task 4 : câbler le shim dans `PtyService` (env PATH + canal) + récepteur dans `Piscine.App`

**Files:**
- Modify: `src/Piscine.App/Terminal/PtyStartInfo.cs` (champ `Environment`)
- Modify: `src/Piscine.App/Terminal/PtyService.cs` (injecte env dans `PtyOptions.Environment`)
- Create: `src/Piscine.App/Terminal/GitPathResolver.cs` (recherche du vrai git hors dossier shim)
- Test: `tests/Piscine.App.Tests/GitPathResolverTests.cs`

- [ ] **Step 1 — Étendre `PtyStartInfo`** : ajouter
  `public IReadOnlyDictionary<string,string>? Environment { get; init; }` (env additionnel/override).

- [ ] **Step 2 — `GitPathResolver`** `src/Piscine.App/Terminal/GitPathResolver.cs` :
  `Resolve(string path, string? excludeDir)` → premier `git`/`git.exe` du PATH dont le dossier n'est
  pas `excludeDir` (comparaison `Path.GetFullPath` insensible casse) ; null sinon. Pur, testable.

- [ ] **Step 3 — `PtyService` injecte l'env** : dans `StartAsync`, construire `options.Environment` =
  env courant (`Environment.GetEnvironmentVariables()`) + overrides de `info.Environment` (PATH préfixé
  par le dossier shim, `PISCINE_REAL_GIT` = `GitPathResolver.Resolve(currentPath, shimDir)`,
  `PISCINE_COACH_PIPE` = endpoint du canal). Si `info.Environment` est null → comportement S2 inchangé
  (rétro-compatible).

- [ ] **Step 4 — Test** `tests/Piscine.App.Tests/GitPathResolverTests.cs` : PATH synthétique avec un
  dossier « shim » (faux `git`) en tête + un dossier « système » (autre faux `git`) →
  `Resolve(path, shimDir)` renvoie le `git` **système** ; cas « aucun git » → null.

- [ ] **Step 5 — Build + test**

Run: `dotnet test tests/Piscine.App.Tests/Piscine.App.Tests.csproj -c Release`
Expected: 0 warning ; `GitPathResolverTests` PASS ; `PtyServiceTests` (S2) toujours verts.

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): PtyService injecte le shim git (PATH + PISCINE_REAL_GIT + canal) + GitPathResolver teste"
```

### Task 5 : boucle de coaching dans la page `/terminal` (panneau + cartes) + E2E Playwright

**Files:**
- Modify: `src/Piscine.Components/Components/Terminal/TerminalPage.razor` (+ `.razor.css`)
- Modify: `src/Piscine.DevHost/Program.cs` (DI : `GitStatusService`, `CoachingService`, `ICoachingChannel`)
- Test: `tests/Piscine.DevHost.E2E/CoachingSmokeTests.cs`

- [ ] **Step 1 — DI dans le DevHost** `src/Piscine.DevHost/Program.cs` : enregistrer
  `GitStatusService`, `CoachingService` (singletons sans état) et `NamedPipeCoachingChannel` en
  `ICoachingChannel` singleton.

- [ ] **Step 2 — Brancher la boucle** dans `TerminalPage.razor` : injecter
  `GitStatusService`/`CoachingService`/`ICoachingChannel` ; passer
  `Environment = { ["__SHIM_DIR"] = <dossier shim build>, ["PISCINE_COACH_PIPE"] = channel.Endpoint }`
  à `PtyStartInfo` lors du `StartAsync` (résoudre le dossier shim via
  `src/Piscine.GitShim/bin/<config>/net10.0`, comme le E2E résout la racine via `Piscine.slnx`).
  `channel.Start()` puis s'abonner à `channel.CommandReceived` → `InvokeAsync` :
  `_state = Status.Read(evt.Cwd)` → `_cards = Coach.Evaluate(_state, evt, expectation)` →
  `StateHasChanged`. Afficher un **panneau de statut** (branche, indexés, non poussés, conflits) et la
  **liste de cartes** (`data-hint-id="@card.Id"`) à côté du `<Terminal>`. Garde dev-only conservée.

- [ ] **Step 3 — CSS** `TerminalPage.razor.css` : layout 2 colonnes (terminal | panneau), styles de
  carte par sévérité. (CSS scopé RCL se bundle seul — piège S1, pas de clé `@Assets` manuelle.)

- [ ] **Step 4 — Vérif visuelle (outils preview)** : `dotnet run --project src/Piscine.DevHost --urls
  http://localhost:5250`, `preview_start` sur `/terminal`, dans le terminal `git init` puis `git commit
  -m x` (rien stagé) → la carte « Rien à committer » apparaît. `preview_screenshot` comme preuve.

- [ ] **Step 5 — E2E Playwright** `tests/Piscine.DevHost.E2E/CoachingSmokeTests.cs` : réutiliser le
  squelette `TerminalSmokeTests` (démarrage DevHost, poll, **skip propre sans Chromium**, racine via
  `Piscine.slnx`, port dédié). Naviguer `/terminal`, focus xterm, taper `git init` + Enter puis
  `git commit -m x` + Enter, attendre la carte `[data-hint-id="commit_nothing_staged"]`.

- [ ] **Step 6 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium). Vérif locale possible via `preview_*` (Step 4).

- [ ] **Step 7 — Commit**

```bash
git add -A
git commit -m "feat(v4): boucle de coaching dans /terminal (panneau statut + cartes) + E2E declenchant un indice"
```

### Task 6 : vérification globale + garde-fous + PR

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** (WarningsAsErrors ; surveiller NU1605/natifs) ; tous les tests verts
(169 S2 + GitStatusService + CoachingService (1/règle) + GitPathResolver + shim relay ; E2E/PTY se
sautent proprement sans Chromium/PTY en CI).

- [ ] **Step 2 — Garde-fous (moteur intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. (Le shim est un projet **neuf** ; confirmer qu'il reste hors de la release
console, comme `Piscine.DevHost`.)

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — PR** (commit et push en **appels séparés** — règle du repo)

```bash
git push -u origin v4/s3-git-coaching
gh pr create --base main --title "v4 S3 — moteur statut git + coaching (shim git + etat depot)" --body-file <fichier>
```

---

## Self-review (couverture S3 vs issue #24 / spec §5–§6)

- **Objectif** « statut git par exercice + coaching déterministe » → `GitStatusService` (T1, pur) +
  `CoachingService` (T2, pur). ✅
- **Périmètre** : `GitStatusService` LibGit2Sharp ✅ T1 · `CoachingService` = tableau §5 ✅ T2 · shim
  `git` en tête de PATH émettant `argv`/`exitCode`/`cwd` par **named pipe**, **sans parsing stdout** ✅
  T3 · câblage `PtyService` env ✅ T4 · boucle de coaching à côté du terminal ✅ T5.
- **Critères d'acceptation** : règles **unit-testées xUnit** (1 `[Fact]`/règle) ✅ T2 · **E2E déclenchant
  un indice** ✅ T5 · **agnostique au shell** (CoachingService ne lit que argv/exitCode/état) ✅ T2.
- **Dépendances** : S1 (moteur/squelette App) + S2 (terminal embarqué où câbler) — réutilisés ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors 0 warning / NU1605-natifs surveillés (T6) · moteur
  (`Core`/`Grading`/`Git`) + `Piscine.Cli` + `release.yml` **INTACTS** (shim = projet neuf hors release,
  T6) · `@rendermode InteractiveServer` (page /terminal, conservé) · Playwright skip-sans-Chromium +
  racine via `Piscine.slnx` (T5) · `GitFixtureBuilder` réutilisé pour les fixtures git (T1) · CSS scopé
  RCL auto-bundlé, pas de clé `@Assets` manuelle (T5) · fenêtre Photino native non vérifiable par un
  agent → preuve = DevHost + Playwright + preview.
- **Repli intégré** : si le canal est instable (app fermée / charge), le shim relaie git
  **transparemment** (timeout 150 ms, fire-and-forget) et le coaching **dégrade en lecture d'état seule
  sans événement de commande** — git n'est jamais altéré.
- **Non-déterminismes maîtrisés** : `GitStatusService` lit l'état disque sans cache, **après**
  l'événement shim (post-exit) ; toutes les règles testées sur des `RepoState`
  construits/`GitFixtureBuilder` → logique indépendante du timing.
