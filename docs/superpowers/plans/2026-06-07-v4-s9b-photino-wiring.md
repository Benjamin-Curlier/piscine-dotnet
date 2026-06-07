# Plan — v4 S9b : câbler l'hôte Photino sur les services App (Router + pages + DI)

> Issue **#42** (milestone #2 « v4 », label `v4`). Branche : `v4/s9b-photino-wiring`.
> Dépendances : **S1–S9 mergées** (`main`, 246 tests). **Débloque #31 (S10 docs flux desktop).**
> Spec : [`../specs/2026-06-06-v4-photino-desktop-design.md`](../specs/2026-06-06-v4-photino-desktop-design.md).
> Pièges : [[piscine-v4-blazor-photino-gotchas]] + HANDOFF « Pièges v4 ».

## Pourquoi ce sprint (constat)
L'app packagée `Piscine.Desktop` est **encore le spike S1** : `src/Piscine.Desktop/App.razor` ne monte
qu'un `<MarkdownView>` avec un markdown **en dur** ; `Program.cs` n'enregistre que `MarkdownRenderer`
(pas de Router, pas de `CourseCatalog`, aucun service `Piscine.App`). Tout le flux recrue (cours/check/
progress/init/resultat) existe et est **prouvé dans `Piscine.DevHost`** (harnais Blazor Server, hors
release) mais **jamais monté dans l'hôte Photino livré** (suivi (b), jamais sprinté). v4 = *remplacer*
l'UX console par le desktop → tant que ce câblage n'est pas fait, le but de v4 n'est pas atteint et la
doc S10 (#31) ne peut pas décrire un « flux desktop de bout en bout ».

## Décisions de tête (recherche faite le 2026-06-07, code lu)
- **(a) Render mode host-agnostique = LE déblocage.** 5 pages RCL portent `@rendermode InteractiveServer`
  (`CheckPage`, `ProgressPage`, `InitPanel`, `PushResultPanel`, `TerminalPage`). C'est un concept Blazor
  **Web App** (`blazor.web.js`, SignalR). Photino rend **in-process via WebView** (`blazor.webview.js`,
  **pas** de render mode) → ces directives **casseraient** dans Photino. Fix : **retirer `@rendermode`
  des 5 pages RCL** et appliquer l'interactivité **globalement côté DevHost** sur le routeur
  (`<Routes @rendermode="InteractiveServer" />` dans `DevHost/Components/App.razor`). Résultat : RCL
  agnostique de l'hôte ; DevHost reste interactif (E2E intacts) ; Photino rend les pages en in-process.
- **(b) DI Photino = port de `DevHost/Program.cs`** MOINS les services du terminal. À enregistrer
  (singletons) : `CourseCatalog`, `MarkdownRenderer`, `PiscineLayout` (même lambda env que DevHost :
  `PISCINE_CONTENT`/`PISCINE_HOME`/`PISCINE_WORKSPACE`), `CheckService(layout, Graders.Default())`,
  `ProgressService(layout, GitStatusService)`, `GitStatusService` (lecture seule, requis par
  ProgressService), `InitService(layout, exe)`, `IPushResultWatcher = ProgressFileWatcher(layout)`.
  **PAS** `PtyService`/`CoachingService`/`ICoachingChannel`/`IHostEnvironment` (terminal = hors périmètre).
- **(c) Terminal/coaching = HORS périmètre** (suivi). Dépend de `Piscine.GitShim` **non empaqueté**
  (`ResolveShimDir` cherche `src/Piscine.GitShim/bin/...` via `Piscine.slnx` → absent du zip) + injecte
  `IHostEnvironment` (non fourni par Photino). **NavMenu ne lie PAS `/terminal`** et Photino n'a pas de
  barre d'URL → `TerminalPage` reste **inatteignable** dans l'app packagée (jamais instanciée → pas de
  crash DI). On la **garde dans la RCL** (DevHost s'en sert), on retire juste son `@rendermode` (T1).
- **(d) Chemin du `content/` dans le zip.** Le zip pose `content/` à la **racine** mais
  `Piscine.Desktop(.exe)` dans **`desktop/`** → `content/` est à `../content` relatif à l'exe desktop.
  Vérifier la résolution `CourseCatalog.ContentRoot` ; si elle ne trouve pas `../content`, faire pointer
  les lanceurs `start-piscine-desktop.{cmd,sh}` (livrés en S9) sur `PISCINE_CONTENT=<racine>/content`.
  (Pour le smoke dev `dotnet run`, exporter `PISCINE_CONTENT=$PWD/content`.)

## ⚠️ Risques & garde-fous
- **Modifs autorisées** : `src/Piscine.Desktop/**`, `src/Piscine.Components/**` (RCL = UI, c'est l'objet),
  `src/Piscine.DevHost/Components/App.razor` (render mode global), `build/launchers/**` (si (d)), `docs/**`.
- **NE PAS toucher le moteur source** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`,
  `src/Piscine.Cli` (comportement inchangé). **NE PAS toucher `release.yml`** (S9 fait ; ajouter au plus
  un `PISCINE_CONTENT` dans les lanceurs si (d) l'exige).
- **Build solution 0 warning** (WarningsAsErrors) ; **tests verts** (dont DevHost E2E avec le render mode
  global) ; `validate-content` OK ; **aucun tag**.
- **Fenêtre native non vérifiable par l'agent** → smoke = « se lance, reste vivant ~12 s, 0 exception ».

## Carte des fichiers
| Fichier | Action |
|---|---|
| `src/Piscine.Components/Components/{Check/CheckPage,Progress/ProgressPage,Init/InitPanel,Push/PushResultPanel,Terminal/TerminalPage}.razor` | T1 — retirer la ligne `@rendermode InteractiveServer` |
| `src/Piscine.DevHost/Components/App.razor` | T1 — `<Routes @rendermode="InteractiveServer" />` (interactivité globale) |
| `src/Piscine.Desktop/Program.cs` | T2 — porter la DII (CourseCatalog + services App, sans terminal) |
| `src/Piscine.Desktop/App.razor` | T3 — remplacer le MarkdownView statique par `<Router>` + `MainLayout` |
| `src/Piscine.Desktop/wwwroot/index.html` | T3 — vérifier assets (theme.css/js + highlight déjà là ; pas d'xterm) |
| `build/launchers/start-piscine-desktop.{cmd,sh}` | T3 (cond. (d)) — `PISCINE_CONTENT` vers `<racine>/content` |
| `docs/superpowers/retex/2026-06-07-v4-s9b-photino-wiring.md` | T5 — retex + checklist smoke proprio |

---

## T1 — Render mode host-agnostique (déblocage, gating)
- [ ] Retirer la ligne `@rendermode InteractiveServer` des **5** pages RCL (cf. carte). Ne rien retirer
  d'autre (les `@inject`, `@using`, `@page`, `@namespace` restent).
- [ ] `DevHost/Components/App.razor` : `<Routes />` → `<Routes @rendermode="InteractiveServer" />`.
- [ ] **Build** `dotnet build Piscine.slnx -c Release` → 0 warning.
- [ ] **Tests DevHost E2E** (les pages doivent rester interactives) :
  `dotnet test src/Piscine.DevHost.E2E -c Release` → verts (+ bUnit Components verts).
  Si une page n'est plus interactive (HeadOutlet/PageTitle, FocusOnNavigate) → vérifier si
  `<HeadOutlet @rendermode="InteractiveServer" />` est aussi requis.

Expected : RCL sans aucun `@rendermode` ; DevHost interactif global ; tous tests verts.
Commit : `refactor(rcl): render mode interactif global (DevHost), pages RCL agnostiques de l'hôte`

## T2 — DI de l'hôte Photino (port DevHost sans terminal)
- [ ] `src/Piscine.Desktop/Program.cs` : enregistrer (singletons) CourseCatalog, MarkdownRenderer,
  PiscineLayout (lambda env identique à DevHost), GitStatusService, CheckService, ProgressService,
  InitService, IPushResultWatcher=ProgressFileWatcher. (Copier les `using` de DevHost/Program.cs.)
- [ ] Build → 0 warning (vérifie que toutes les deps des pages routées sont satisfaites).

Commit : `feat(desktop): enregistrer CourseCatalog + services Piscine.App dans l'hôte Photino`

## T3 — Router + layout dans l'hôte Photino
- [ ] `src/Piscine.Desktop/App.razor` : remplacer le `<MarkdownView>` statique par un `<Router>` calqué
  sur `DevHost/Routes.razor` : `AppAssembly="typeof(Program).Assembly"` (ou `typeof(App).Assembly`),
  `AdditionalAssemblies="new[]{ typeof(Piscine.Components.MarkdownView).Assembly }"`,
  `NotFoundPage="typeof(Piscine.Components.Components.Pages.NotFound)"`, `<Found>` →
  `<RouteView DefaultLayout="typeof(Piscine.Components.Components.Layout.MainLayout)" />`. Importer les
  `@using` Blazor routing nécessaires (le SDK Razor de Desktop a déjà `ImplicitUsings`).
- [ ] `wwwroot/index.html` : confirmer `_content/Piscine.Components/{css/piscine.css,js/theme.js}` +
  highlight (déjà présents en S6). Pas d'xterm (terminal hors périmètre).
- [ ] **(d) content path** : `dotnet run --project src/Piscine.Desktop -c Release` avec
  `PISCINE_CONTENT=$PWD/content` ; si la résolution sans env échoue dans la disposition du zip
  (`desktop/` + `../content`), ajouter `PISCINE_CONTENT` aux lanceurs `start-piscine-desktop.{cmd,sh}`.

Commit : `feat(desktop): monter le Router RCL + MainLayout dans l'hôte Photino`

## T4 — Vérification (build + tests + smoke Photino)
- [ ] `dotnet build Piscine.slnx -c Release` → 0 warning.
- [ ] `dotnet test Piscine.slnx -c Release` → verts (compteur ≥ 246 ; noter le total).
- [ ] `validate-content` → « Contenu valide. ».
- [ ] **Smoke Photino** (background, ~15 s, `PISCINE_CONTENT=$PWD/content`) :
  `dotnet run --project src/Piscine.Desktop -c Release` → **0 exception** dans la sortie, process vivant.
  (La fenêtre + le routage visuel = vérif proprio.) Tuer le process ensuite.
- [ ] **Garde moteur** : `git diff --name-only origin/main...HEAD -- src/Piscine.Core src/Piscine.Grading
  src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml` → **vide**.

## T5 — Revue + retex + PR (sans tag)
- [ ] Revue indépendante (focalisée RCL/Photino) — feu vert avant merge.
- [ ] Retex `docs/superpowers/retex/2026-06-07-v4-s9b-photino-wiring.md` : décision render mode global,
  DI portée, terminal/shim déféré, content-path, **checklist smoke proprio par OS** (la fenêtre route).
- [ ] PR (push + create, **appels séparés**, pas de tag) ; `gh pr merge --squash --delete-branch` ;
  fermer #42 (« Fixes #42 » en anglais OU `gh issue close`). Consigner (HANDOFF + mémoire).

Expected : l'app de bureau **route** le flux recrue (cours/check/progress/init/resultat) en réutilisant la
RCL ; build 0 warning ; tests verts ; moteur/`Cli`/`release.yml` intacts ; aucun tag. **#31 (S10) débloqué.**

## Self-review (couverture vs #42)
- Router + pages + DI montés dans Photino ✅ (T2/T3) ; render mode rendu host-agnostique ✅ (T1, risque
  dominant) ; terminal/coaching **explicitement déféré** (shim non packagé) ✅ (c) ; content-path du zip
  traité ✅ (d) ; moteur/CLI/release intacts ✅ ; smoke = se lance/0 exception, fenêtre = proprio ✅.
- Pas de gold-plating : pas de terminal Photino, pas de packaging du shim, pas de nouvelles pages.
