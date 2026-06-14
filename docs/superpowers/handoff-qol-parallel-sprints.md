# HANDOFF — épic QoL : exécuter S3→S8 EN PARALLÈLE

> Destiné à un orchestrateur multi-agent (« ultracode » / dispatching parallèle). Permet de lancer
> plusieurs sprints QoL **concurremment**, chacun sur sa branche, sans se marcher dessus. Lis aussi la
> **spec** [`specs/2026-06-13-qol-desktop-epic-design.md`](specs/2026-06-13-qol-desktop-epic-design.md)
> (sections §5.x référencées ci-dessous) et la note d'épic [[piscine-qol-epic]].

## État (au 2026-06-14)
- **Mergé dans `main`** : correctif écran noir Photino (`[STAThread]` + `IConfiguration`), **S0** (fondation nav,
  #85), **S1** (tableau de bord, #87). **S2** (plan de travail exo + « Ouvrir », #88) en cours de merge.
- **Branches par sprint** off `main` : `qol/sN-<slug>`. 1 sprint = 1 PR = merge on green.
- **Smoke de rendu Photino = gate LOCAL uniquement** (CI GitHub-hosted ne peut pas lancer WebView2 GUI ;
  cf. `retex/2026-06-14-desktop-blank-screen.md`). Ne PAS recréer de job CI `desktop-render-smoke`.

---

## LE PLAYBOOK (s'applique à CHAQUE sprint, sans exception)
1. **Brancher off `main` à jour** : `git fetch && git checkout main && git pull && git checkout -b qol/sN-<slug>`.
   (Rebrancher off main *après* chaque merge de sprint amont pour récupérer les fichiers partagés.)
2. **Invariant dur** : ne **jamais** toucher `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`,
   `src/Piscine.Cli`, ni `.github/` / `release.yml`. Le moteur, le CLI headless et `grade-received` sont gelés.
3. **Où va le code** : UI dans la RCL **`src/Piscine.Components`** ; logique sans UI dans **`src/Piscine.App`**.
   DI à enregistrer dans **les DEUX** hôtes (`src/Piscine.Desktop/Program.cs` **et** `src/Piscine.DevHost/Program.cs`).
4. **Pyramide de tests** : pur → **xUnit** (`tests/Piscine.App.Tests`) ; composants → **bUnit** (`BunitContext`,
   `Render<T>()`, `tests/Piscine.Components.Tests`) ; bout-en-bout → **Playwright E2E** sur le DevHost
   (`tests/Piscine.DevHost.E2E`, **skip propre sans Chromium** : `catch (PlaywrightException) { return; }`,
   port dédié unique par fichier — déjà pris : 5247/5249/5251/5253/5255/5257/5259/5261). Suivre le modèle de
   `NavigationSmokeTests.cs`.
5. **Pages interactives** : un `@page` avec `@onclick`/services porte `@rendermode InteractiveServer` (résout via
   l'indirection `InteractiveRenderSettings` de la RCL — ne PAS globaliser le render mode). Modèle : `Dashboard.razor`.
6. **Qualité** : `dotnet build/test Piscine.slnx -c Release` **0 warning** (`TreatWarningsAsErrors`). Commits
   **conventionnels FR** terminés par `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.
   **commit ≠ push** (appels séparés). Sécurité : lancements de process avec args **en tableau** (jamais de
   concaténation), cible = dossier de workspace résolu.
7. **Clôture** : `dotnet test Piscine.slnx -c Release` vert + (local) smoke Photino + `validate-content` OK →
   push → PR FR → **merge on green** → brancher le sprint suivant off main.

---

## CARTE DE PARALLÉLISATION (éviter les conflits)

**Fichiers PARTAGÉS** (plusieurs sprints veulent les éditer → source de conflits) :
- `src/Piscine.Components/Navigation/NavDestinations.cs` — **S5** (ajoute `/rapport`) et **S6** (ajoute `/reglages`)
  ajoutent chacun **une ligne** à `Primary`.
- `src/Piscine.Components/Components/Layout/MainLayout.razor` — **S3** (monte la palette `⌘K`) et **S4** (monte
  un `ToastHost`) insèrent chacun **un élément**.
- `src/Piscine.Components/wwwroot/css/piscine.css` + `js/theme.js` — **S6** (thème/échelle police) et **S7**
  (passe lisibilité/a11y) éditent largement le CSS → **fort risque de conflit**.

**Vagues recommandées :**
- **Vague 1 (parallèle) : S3, S4, S5.** Conflits seulement triviaux et additifs (1 ligne dans `NavDestinations`
  pour S5 ; 1 insertion distincte dans `MainLayout` pour S3 et S4 — ancres différentes : S3 en haut du shell,
  S4 près de `@Body`). Résolution de merge évidente ; **merger dans l'ordre S5 → S3 → S4** et rebaser le suivant.
- **Vague 2 (séquentielle) : S6 puis S7.** Les deux touchent le CSS lourdement → **ne PAS** paralléliser ;
  S6 (réglages + thème/police) d'abord, puis S7 (a11y/lisibilité + onboarding) qui s'appuie sur l'audit S0 et le
  CSS de S6.
- **S8 (docs) en DERNIER**, séquentiel, une fois S3→S7 mergés (il documente le tout).

**Si on veut maximiser le parallélisme** : un mini-commit d'intégration préalable peut ajouter d'un coup les 2
entrées nav (`/rapport`, `/reglages`) et les 2 points de montage `MainLayout` (palette + toast) — alors S3/S4/S5/S6
n'ont plus à toucher ces fichiers partagés et tournent vraiment en parallèle. (Recommandé si l'orchestrateur gère
bien les worktrees.)

---

## BRIEFS PAR SPRINT

### S3 — Palette de commande `⌘K` + recherche + raccourcis (spec §5.6)
- **Quoi** : overlay `CommandPalette` (RCL) ouvert par `Ctrl/⌘+K` → saut flou vers tout module/exercice/destination
  (`NavDestinations.Primary`)/action ; **recherche plein-texte** sur le markdown cours+sujets (index léger en
  mémoire bâti depuis `CourseCatalog`) ; raccourcis (exo suivant/précédent, focus recherche, aller au board).
- **Fichiers** : `Components/CommandPalette.razor`(+css+js interop pour le hotkey global + piège de focus) ; un
  `SearchIndex`/`SearchService` **pur** dans `Piscine.App` (testable xUnit : requête → résultats classés) ;
  montage dans `MainLayout` (partagé). bUnit sur la palette (filtrage), E2E (⌘K → saut).
- **Réutiliser** : `CourseCatalog` (modules/exos), `NavDestinations`.

### S4 — Boucle de retour enrichie (spec §5.7)
- **Quoi** : **diff structuré** attendu/obtenu (aujourd'hui `CheckOutcome.Cases[].Messages` = texte verbatim) →
  exposer un diff structuré depuis `CheckService` (couche App, **sans** toucher au grader moteur) rendu en diff
  coloré dans `CheckFeedback` ; **toast de push global** = un `ToastHost` dans `MainLayout` abonné à
  `IPushResultWatcher.ResultReceived` (s'affiche partout) ; **activité récente** (réutiliser `BoardRecent`).
- **Fichiers** : `Piscine.App/Checking/*` (diff structuré + tests), `Components/Check/CheckFeedback.razor`,
  `Components/Layout/ToastHost.razor` + montage `MainLayout` (partagé). bUnit (diff, toast), E2E.
- **Réutiliser** : `IPushResultWatcher` (`LatestResult`/`ResultReceived`/`LatestRichResult`), `CheckService`,
  `StatusBadge`, `BoardRecent`.

### S5 — Page de rapport + export (spec §5.5)
- **Quoi** : `/rapport` (recrue + encadrant), **lecture seule** : identité git (`GitStatusService`/`RepoState`),
  date, avancement (`BoardCounts`), tableau par module (`ProgressService`), historique push. **Export** : feuille
  `@media print` (PDF/papier) + bouton « Copier/Enregistrer en Markdown ».
- **Fichiers** : `Components/Pages/Report.razor`(+css print) ; un générateur **Markdown pur** dans `Piscine.App`
  (testable xUnit) ; **+1 ligne** dans `NavDestinations.Primary` (`/rapport`, partagé). bUnit (rendu + markdown),
  E2E (page + bouton export).
- **Réutiliser** : `ProgressService.SnapshotFor`, `BoardCounts`, `ProgressRollup`, `GitStatusService`, `IPushResultWatcher`, `CourseCatalog`, `StatusBadge`.

### S6 — Page Réglages + persistance thème + échelle police (spec §5.8)
- **Quoi** : `/reglages` : commande éditeur (déjà dans `SettingsService` — S2), **thème** (persister le choix
  clair/sombre au lieu du localStorage volatile), **échelle de police** (lisibilité), cible terminal par défaut.
  Étendre `AppSettings`/`SettingsService` (champs `Theme`, `FontScale`). Appliquer via `piscine.css`/`theme.js`.
- **Fichiers** : `Components/Pages/Settings.razor`(+css) ; extension `Piscine.App/Settings/*` (+ tests xUnit) ;
  **+1 ligne** `NavDestinations` (`/reglages`, partagé) ; `wwwroot/css/piscine.css` + `js/theme.js` (partagé S7).
  bUnit + E2E.
- **Réutiliser** : `SettingsService`/`AppSettings` (S2), le toggle thème existant.

### S7 — Passe lisibilité/a11y + onboarding 1ᵉʳ lancement (spec §5.8)
- **Quoi** : passe **lisibilité/accessibilité** sur `piscine.css` (contraste AA, états de focus visibles, échelle
  de police, responsive du shell) guidée par l'**audit** `audits/2026-06-13-ux-audit.md` (§« À faire en S7 ») ;
  **onboarding** au 1ᵉʳ lancement (workspace non initialisé → init guidé → 1ᵉʳ exo) enrobant `InitService`.
- **Fichiers** : `wwwroot/css/piscine.css` (large — **après S6**), `Components/Onboarding/*` + montage (1ʳᵉ visite).
  bUnit (onboarding states) + E2E + revue visuelle locale (clair/sombre).
- **Réutiliser** : `InitService`/`InitPanel` (S0/v4), l'audit S0.

### S8 — Docs recrue/encadrant + CHANGELOG (séquentiel, dernier)
- **Quoi** : `mise-en-oeuvre.md`/`deploiement.md`/`Curriculum.md`/wiki reflètent board + workbench + Ouvrir +
  palette + rapport + réglages ; **CHANGELOG « Non publié »**. **Docs uniquement**, `validate-content` OK, aucun tag.

---

## BRIQUES RÉUTILISABLES (déjà livrées — ne pas refaire)
- **Nav** : `Navigation/NavDestinations` (data + `IsActive`), `NavTabs`, `NavMenu` (arbre + pastilles).
- **Statut** : `Piscine.App/Progress/ProgressService.SnapshotFor`, `ProgressRollup.ForModule`,
  `ExerciseProgressStatus`, `StatusBadge`, `StatusDot`, `ExerciseProgressStatusText` (libellé/CSS partagés).
- **Board** : `Piscine.App/Board/{ResumeSelector,BoardCounts,ModuleProgress}`, `BoardOverview`, `BoardRecent`, `Dashboard`.
- **Ouvrir/exo** : `Piscine.App/Launch/{IProcessLauncher,ProcessLauncher,EditorResolver,ExecutableProbe,WorkspaceLauncher}`,
  `Piscine.App/Settings/{AppSettings,SettingsService}`, barre d'action `Exercise.razor`.
- **Check/push** : `Piscine.App/Checking/CheckService` + `CheckFeedback` ; `Piscine.App/Push/IPushResultWatcher`.
- **Contenu** : `CourseCatalog` (modules/exos/markdown), `CourseToc`, `MarkdownView`.
- **Moteur (lecture seule, ne pas modifier)** : `ContentLocator.FindExercise`, `StarterInstaller.Install`,
  `PiscineLayout` (`Content`, `WorkspaceExerciseDir`, `StateDir`, `ProgressPath`, `LastPushResultPath`).

## Pièges connus (cf. [[piscine-v4-blazor-photino-gotchas]])
- Render modes RCL via indirection `InteractiveRenderSettings` ; `_Imports.razor` hôte requis ; clés `@Assets`
  préfixées `_content/<RCL>/`. Photino = `[STAThread]` Main + `IConfiguration` (déjà en place — **ne pas régresser**).
- E2E Playwright : résoudre la racine via `Piscine.slnx`, port dédié, skip-sans-navigateur, tuer l'arbre au Dispose.
