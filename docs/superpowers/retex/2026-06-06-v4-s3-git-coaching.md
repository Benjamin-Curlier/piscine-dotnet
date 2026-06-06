# Retex — v4 S3 : moteur statut git + coaching (shim git + état dépôt)

> Issue #24. Branche `v4/s3-git-coaching`. Plan : [../plans/2026-06-06-v4-s3-git-coaching.md](../plans/2026-06-06-v4-s3-git-coaching.md).
> **Verdict : objectif atteint.** Statut git déterministe + coaching par règles, prouvés bout-en-bout
> sur Windows (unitaire + intégration IPC + E2E plein-stack + vérif visuelle). Linux/macOS = checklist
> proprio. Revue agent : **feu vert, 0 bloquant**.

## Décisions techniques

- **Canal shim → app = named pipe .NET** (`NamedPipe{Server,Client}Stream`) : cross-platform (socket
  domaine Unix sous Linux/macOS), pas de port/firewall, nom de pipe **unique par session** (`Guid`,
  non devinable). Loopback TCP = repli derrière `ICoachingChannel` si besoin. Le shim écrit **une ligne
  JSON** `{argv,exitCode,cwd}` puis se déconnecte — *fire-and-forget*, ne bloque **jamais** git.
- **Résolution du vrai git sans récursion** : `PtyService` fixe `PISCINE_REAL_GIT` (chemin absolu,
  hors dossier shim, via `GitPathResolver`) ; le shim l'exécute. Défense secondaire : recherche PATH en
  s'excluant (`Environment.ProcessPath`). **Le shim ne résout/lance que `git.exe`/`git` (PAS `git.cmd`)**
  car `UseShellExecute=false` ne lance pas un `.cmd` ; `proc.Start` est de plus gardé (→ 127 + stderr,
  jamais de crash). Sous Windows le vrai git = **MinGit** (livré dans le zip).
- **cwd isolée par session** : la page `/terminal` lance le PTY dans un dossier temp neuf
  (`%TEMP%/piscine-term-<guid>`), **jamais** le cwd de l'app (= le vrai repo) → `git init`/`commit` du
  coaching sont sandboxés et déterministes. Lecture d'état via `evt.Cwd` (rapporté par le shim).
- **Pas de parsing de stdout** (piège spec écarté) : le coaching ne lit que l'événement structuré du
  shim (`argv`/`exitCode`/`cwd`) + l'état LibGit2Sharp. `CoachingService` est **pur et agnostique au
  shell**.

## Ce qui est PROUVÉ sur Windows (par l'agent, automatique)

- `GitStatusService` : 10 tests unitaires (fixtures `GitFixtureBuilder` + manips LibGit2Sharp) — branche,
  détaché, compteurs stagé/non-stagé/non-suivi, origin, ahead, marqueurs de conflit en début de ligne
  (+ anti-faux-positif).
- `CoachingService` : 1 `[Fact]` par règle du tableau spec §5 (8) + état propre → 0 carte + ordre de
  priorité. Pur, agnostique au shell.
- **Shim IPC** : test d'intégration `GitShimRelayTests` — le shim relaie le code de sortie d'un faux git
  (exit 3) **et** émet l'événement reçu par le `NamedPipeCoachingChannel` (a tourné 4/4, ~90 ms).
- **Plein-stack** : E2E Playwright `CoachingSmokeTests` — taper `git init` puis `git commit -m x` dans
  le terminal → la carte `commit_nothing_staged` apparaît (xterm → PTY → shim → named pipe → coaching →
  DOM). A tourné et passé sur Chromium + MinGit réels.
- Vérif visuelle (preview) : `/terminal` rend le terminal + le **panneau de coaching** avec statut live
  (« Pas encore de depot (lance git init) ») dans la cwd temp isolée.
- Build solution **0 warning** ; **195 tests verts** ; moteur/`Cli`/`release.yml` intacts ; shim = projet
  neuf hors release.

## Checklist smoke par OS — **À EXÉCUTER PAR LE PROPRIO**

Un agent ne valide que Windows. `dotnet build -c Release` (pour bâtir le shim) puis
`dotnet run --project src/Piscine.DevHost` → `http://localhost:5244/terminal` :

- [x] **Windows** : `git init` puis `git commit -m x` → carte « Rien à committer » ; panneau statut MAJ. *(prouvé)*
- [ ] **Linux** : named pipe = socket domaine Unix ; shim `git` (forkpty Porta.Pty) relaie le git système ;
  vérifier que la carte apparaît. (Photino plus tard : `libwebkit2gtk` — cf. S9.)
- [ ] **macOS** : idem ; git système ; vérifier carte + panneau.

## Limites connues (revue de sprint — acceptables, à suivre)

- **Parsing de sous-commande** : `GitCommandEvent.Subcommand` saute `-c`/`-C` mais pas les options
  globales à valeur séparée (`--git-dir X`, `--work-tree X`…) → `git --git-dir /x status` mal détecté.
  Impact faible (les recrues tapent `git commit`/`add`/`push` ; les formes `--opt=val` passent). À durcir
  si on coache sur ces flags.
- **Fuite de dossier temp au drop de circuit dur** : `/terminal` supprime `%TEMP%/piscine-term-*` en
  `DisposeAsync`, mais un drop SignalR brutal peut ne pas l'appeler. Acceptable (harnais dev) ; balayage
  des `piscine-term-*` au démarrage si ça devient bruyant. (Inhérent à l'hôte SignalR → non applicable à
  Photino.)
- **`git.cmd` non supporté** comme vrai git (Windows utilise MinGit `git.exe`) — choix assumé
  (UseShellExecute=false). Garde de lancement en place (127 + stderr, pas de crash).

## Pièges réutilisables (pour le HANDOFF / sprints suivants)

- **IPC inter-process .NET cross-platform** = `System.IO.Pipes` named pipe (BCL, pas de NuGet). Créer le
  **premier** `NamedPipeServerStream` **synchroniquement** dans `Start()` avant de spawn le client, sinon
  course au 1er appel (le `Connect(150)` du client peut précéder la création du serveur).
- Un exe qui doit **intercepter une commande** (shim) : `AssemblyName=git`, en tête de `PATH` via
  `PtyOptions.Environment` (dict **insensible à la casse** pour ne pas dupliquer `PATH`/`Path` sous
  Windows) ; résoudre la vraie cible **hors de son propre dossier** (`Environment.ProcessPath`).
- Relais de process transparent : `ProcessStartInfo{UseShellExecute=false}` hérite stdin/stdout/stderr
  (git interactif marche) ; capturer le code de sortie **avant** tout effet de bord (émission) ; garder
  `proc.Start` en try/catch (cible non lançable → code non nul, pas de crash).
- Tests E2E qui lancent chacun un `dotnet run` : **désactiver la parallélisation** xUnit du projet
  (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`) sinon contention build/locks.
- Layout 2 colonnes Blazor : `grid-template-columns: minmax(0,1fr) <pane>` laisse le panneau **écraser**
  la 1ʳᵉ colonne à ~0 quand l'espace manque → empiler par défaut + 2 colonnes via `@media (min-width)`
  avec un **min réel** sur la colonne principale.
