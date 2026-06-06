# HANDOFF — reprise à froid

> Point d'entrée pour reprendre le projet **sans contexte préalable**. Dernière MAJ : 2026-06-06.
> Lis aussi `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md` (design complet) et
> `docs/wiki/Curriculum.md` (état du curriculum).

## C'est quoi
Bootcamp « piscine » façon Epitech/42 pour **C# / .NET 10**. Livrable = zip autonome (cours +
exercices + appli console qui sert d'UX recrue ET de **moulinette auto-correctrice** locale, sans
SDK). Rendu via **vrai git** (dépôt bare local + hook `post-receive`). Correction **par groupe,
séquentielle, stop au 1er KO** ; **retour éducatif, jamais de note**. Exercices **data-driven**
(`manifest.yaml`) → ajout sans recompiler.

Repo : https://github.com/Benjamin-Curlier/piscine-dotnet (privé). Solution **`Piscine.slnx`** (.NET 10).
Branche par défaut **`main`**. Commits conventionnels en **français**, terminés par
`Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## État actuel (résumé)
- **Release `v2.0.0`** (tag poussé, `release.yml` vert, 3 zips win/linux/osx attachés). Bundle
  cumulatif depuis `v1.0.0` : graders **git / projet / reseau / mutation**, modules **M19/M20**
  débloqués, palier **v2 (M24–M35)** et plateformes **v3 (M36–M39)**, **Rush 3**, site Blazor.
  Procédure de release : [docs/deploiement.md](../deploiement.md) ; détail : [CHANGELOG.md](../../CHANGELOG.md).
  (Première release `v1.0.0` = curriculum M00–M23 + Rushes 0/1/2, blocages branchés.)
- **Moteur** : graders `io` / `unit` / `norme` / **`mutation`** / **`git`** (Roslyn embarqué, refs via
  `TRUSTED_PLATFORM_ASSEMBLIES`), rendu git, scaffolder `piscine new exercise`, gate `validate-content`.
  - **`git` = grader « état attendu du dépôt »** (Sprint 1 V3, issue #1, fondation moteur FAITE) : le
    manifest décrit un bloc `git:` (branches présentes, `min_commits`, `merged` ancêtre, `files`
    contains/content, `no_conflict_markers`) ; `GitGrader` inspecte le dépôt rendu via **LibGit2Sharp**
    (`GradingContext.RepositoryPath`) → verdict éducatif unique, trigger `git_state`. **Adoption
    contenu restante** (issue #9) : extraire le **vrai dépôt** côté `grade-received` (aujourd'hui
    snapshot plat sans `.git`), exos pilotes M05/M14, fixtures `validate-content`.
  Binaire embarque `Microsoft.Extensions.*` (DI/Logging/Hosting) **et** `Microsoft.EntityFrameworkCore.Sqlite`.
  - **`mutation` = grader « élève-écrit-tests »** (Point 6 des blockers, FAIT) : la recrue livre des
    tests xUnit ; le moteur les confronte à une impl de référence cachée (`reference/`) + des mutants
    (find/replace nommés dans le manifest) ; verdict binaire (tous les mutants tués). Mutant survivant
    → label du cas manquant. `MutationGrader` + `XunitRunner` partagé avec `unit`. Pilote : **M13
    `ex03-mutation`** (`Compte.Retirer`, 2 mutants).
- **Contenu** : modules **M00–M35** + **M36 (Clean Architecture, V3)** + **M37 (Docker, lecture, V3)**
  + **M38 (Silk.NET, lecture, V3)** + **Rushes 0/1/2/3**.
  - Auto-notés `io` : M00–M04, M06–M13, **M15–M21**, M23, M24–M33, M35.
  - Auto-noté `projet` (V3) : **M36** (`ex00-couches`).
  - Auto-noté `git` (V3) : **M05** (`ex00-branche-merge`, validé par fixture).
  - Lecture (V3) : **M37** (Docker), **M38** (Silk.NET), **M39** (Blazor).
  - Lecture guidée (cours seul, groupe `exercises: []`) : **M14, M22, M34** (M05 désormais auto-noté `git`).
  - **M19 (Logging) & M20 (Generic Host) DÉBLOQUÉS** (post-v1.0, en `io`) — cf. ci-dessous.
- **Site du cours** : `src/Piscine.DevHost` (ex-`Piscine.Web`, renommé en v4 S1 ; Blazor .NET 10, type
  Docusaurus + **mode sombre**) présente cours+sujets dans le navigateur ; harnais de test/dev des
  composants. `dotnet run --project src/Piscine.DevHost` → http://localhost:5244. Dans la slnx (buildé
  par la CI), **hors release**. Les composants/services de rendu vivent désormais dans la RCL
  `src/Piscine.Components` (cf. v4 ci-dessous).
- **Tests** : 246 verts (`dotnet test Piscine.slnx -c Release` : Core 46 + App 51 + Components 23 + Git 7 +
  DevHost.E2E 8 + Grading 111). CI GitHub Actions verte sur `main`. (164 avant v4 ; +3 S1 ; +2 S2 ; +26 S3 ;
  +9 S4 ; +15 S5 ; +6 S6 ; +10 S7 ; +11 S8 = PushResultWatcher 6 + PushResultPanel bUnit 4 + E2E /resultat 1.)
- Arbre propre, tout poussé. Dernier commit `main` : voir `git log -1`.

## Architecture moteur (l'essentiel)
- `src/Piscine.Core` : modèles + découverte de contenu (logique pure, testée).
- `src/Piscine.Grading` : `CompilationService` (Roslyn), graders, `GroupGrader` (stop au 1er KO ;
  un exo `bonus` à revoir ne bloque PAS la suite), `ContentValidator` (gate).
- `src/Piscine.Git` : rendu git (LibGit2Sharp), `grade-received`.
- `src/Piscine.Cli` : orchestration/affichage (`AssemblyName=piscine`). Commandes :
  `list`, `start <exo>`, `check <exo>`, `try <exo>`, `status`, `init`, `grade-received <sha>`,
  `validate-content`, `package-content <src> <dest>`, `new exercise <module> <id>`.
- `src/Piscine.Components` (v4 S1) : **RCL** partagée — composants Razor (pages cours/module/exercice,
  layout, `MarkdownView`) + services de rendu (`CourseCatalog`, `MarkdownRenderer`, Markdig). Consommée
  par `Piscine.DevHost` et `Piscine.Desktop`.
- `src/Piscine.DevHost` (ex-`Piscine.Web`) : site du cours (Blazor SSR) + harnais de test des composants.
  Réutilise la RCL ; coloration highlight.js, mode sombre. N'est PAS dans le binaire `piscine` packagé.
- `src/Piscine.App` (v4 S1) : squelette de la couche services (réf. Core/Grading/Git ; se remplit en S3+).
- `src/Piscine.Desktop` (v4 S1) : hôte **Photino.Blazor** (livré à terme) qui rend la RCL ; spike OK.

## Format d'un module de contenu
`content/modules/NN-slug/` = `module.yaml` (`id`, `title`, `order`, `course`, `groups[].exercises[]`)
+ `cours.md` + `exercises/exNN-<id>/` { `manifest.yaml`, `subject.md`, `starter/<F>.cs`, `solution/<F>.cs` }.
Un **Rush** : `content/rushes/<id>/` avec le `manifest.yaml` DIRECTEMENT dedans (pas de groupe).
Un **module de lecture** : `module.yaml` avec un groupe `exercises: []` (passe la gate sans exo).

`manifest.yaml` : `id, title, objective, difficulty(facile|moyen|difficile), bonus(bool, optionnel),
deliverables, starter, grading[{type: io, cases:[{stdin, expect_stdout, expect_exit}]}],
feedback{hints, course_ref}, solution`.

## RÈGLES CRITIQUES du grader (sinon le corrigé casse la gate)
1. **Aucun implicit using** côté Roslyn : tout `using` autre que `System.Console` doit être
   **explicite** ; `System.Console.WriteLine/ReadLine` toujours **pleinement qualifié**.
2. **Top-level statements d'abord**, classes/types **après**.
3. `expect_stdout` en YAML : **double-quoté avec `\n`** (le grader normalise `\r\n`→`\n`). Pour du JSON,
   échapper les guillemets : `"{\"Nom\":\"Alice\"}\n"`.
4. Sorties **déterministes** (pour le concurrent : résultat indépendant de l'ordre / FIFO).
5. Packages dispo dans le grader : `Microsoft.Extensions.DependencyInjection/Logging/Hosting`,
   `Microsoft.EntityFrameworkCore.Sqlite`. (EF en `io` = **SQLite in-memory** déterministe, avec `ORDER BY`.)

## MÉTHODE pour ajouter du contenu (pas de changement moteur)
1. Écrire le corrigé `solution/<F>.cs` **d'abord** (avant de remplir `expect_stdout`).
2. **NE PAS deviner `expect_stdout`** : laisser les `expect_stdout` vides/approximatifs, puis lancer
   `&$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- try <exo>`
   → exécute le corrigé sur chaque `stdin` et **imprime le stdout réel au format YAML collable**
   (`\n` littéral, guillemets échappés). Copier ces lignes `expect_stdout`/`expect_exit` dans le manifest.
   Boucle interne ciblée (1 exo) ; détecte aussi tôt les violations des règles grader (compile KO).
3. Gate global final : `... -- validate-content` → doit afficher **« Contenu valide. »**.
4. `git commit` puis `git push origin main` (**appels séparés** — un refus du push perd le commit).
5. CI : `gh run list --branch main --limit 1 --json status,conclusion`.
- Réserver `dotnet test Piscine.slnx -c Release` aux itérations qui touchent `src/`.
- **Délégation** : pour créer plusieurs modules en parallèle, lancer des **agents `sonnet`**
  (les agents `opus` ont calé sur une limite de session), prompts étroits avec specs exactes
  (stdin/stdout au caractère près). Valider une fois à la fin, puis 1 commit par module/rush.

## PROCHAINES ÉTAPES (par ordre)
**v2 — approfondissement C#/.NET : CONTENU TERMINÉ (M24–M35), CI verte.** Tous `io` (5 exos dont
1 bonus difficile) sauf **M34 = lecture**. Créés en séquentiel (pas de sous-agent), via l'outil
auteur **`piscine try <exo>`** (génère `expect_stdout`, ne plus deviner) : M24 switch, M25 enums,
M26 static/const, M27 binaire, M28 complexité/tris, M29 recherche de chemin (BFS/Dijkstra/A*),
M30 design patterns suite (Singleton/Adapter/Decorator/Builder/Command), M31 refactoring
(starter smelly, io = filet de régression), M32 GC/ressources (IDisposable/`using`),
M33 discriminated unions (`abstract record` + `sealed`), M34 interop (lecture), M35 EF Core
(SQLite in-memory, `ORDER BY` déterministe).
- **M19/M20 FAITS** (débloqués en **contenu pur**, sans changement moteur) : voir encadré ci-dessous.
- **PROCHAINE TÂCHE = ENRICHISSEMENT** : ajouter **1 exo bonus (difficile, non bloquant)** par module
  **fondamental** M00–M18, M21, M23 (qui n'ont que 3 exos sans bonus ; les modules v2 M24+ ont déjà
  leur bonus). Vague recommandée d'abord : **cœur débutant M01–M08** (~8 exos), puis le reste.
  Méthode = section « MÉTHODE pour ajouter du contenu » ci-dessus (écrire le corrigé, `try` pour
  générer `expect_stdout`, ajouter `difficulty: difficile` + `bonus: true` au manifest, l'ajouter à la
  fin de la liste `exercises:` du `module.yaml`, `validate-content`, 1 commit/module).
- Penser à **MAJ `docs/wiki/Curriculum.md`** au fil des nouveaux exos.

**Comment M19/M20 ont été débloqués (réutilisable pour les enrichir).** Le provider console de
`Microsoft.Extensions.Logging.Console` écrit en arrière-plan et contourne le `Console.SetOut` du
grader → sortie non capturée/non déterministe. Solution **contenu** : un `ILoggerProvider` synchrone
**fourni** (`LogCapture.cs`, livrable additionnel à côté du `Program.cs`) écrit via
`System.Console.WriteLine` au format `Catégorie [Niveau] message` → capté, déterministe. Le grader
compile **tous les livrables** (`manifest.Deliverables`), donc le fichier fourni est compilé avec le
programme. M20 réutilise ce provider + arrêt déterministe via `IHostApplicationLifetime.StopApplication()`
(services « single-shot ») + `AddFilter("Microsoft", LogLevel.None)` pour taire les logs du host.

**Branche `v1.0-blockers`** — `docs/superpowers/BLOCKERS-v1.0.md` détaille les chantiers moteur, avec
cause/résolution/effort. État :
- ✅ **Point 1/2** (logging M19 / Generic Host M20) — débloqués en contenu pur (cf. encadré ci-dessus).
- ✅ **Point 6** grader **« élève-écrit-tests »** (M13, mutants) — FAIT (type `mutation`, voir Moteur ci-dessus ;
  spec `docs/superpowers/specs/2026-06-05-grader-mutation-design.md`, plan `.../plans/2026-06-05-grader-mutation.md`).
- ⏳ **Restants** : grader **git** → **fondation moteur FAITE** (Sprint 1 V3) ; **adoption contenu**
  (issue #9) ; **Rush 3** (worker déterministe) → **FAIT** (Sprint 2 V3, `r3-traitement`) ;
  **harnais réseau** (M22) → Point 5.

**v3 — plateformes & architecture** (nouveau **grader « projet »** multi-fichiers/web) :
Blazor, Silk.NET, Docker, clean architecture. Voir `docs/superpowers/plans/2026-05-31-roadmap-v2-v3.md`.
- **SCRUM / boucle V3 : backlog = GitHub Issues** (milestone « V3 — plateformes & architecture »,
  label `v3`). 1 sprint = 1 issue (plan → impl → revue agent → docs → retex → PR mergée).
  - ✅ **#1 Grader git (fondation moteur)** — FAIT (PR #10, plan
    `docs/superpowers/plans/2026-06-06-grader-git.md`). Tests **144** verts.
  - ✅ **#2 Rush 3 (worker déterministe)** — FAIT (PR #11, `content/rushes/r3-traitement`, plan
    `docs/superpowers/plans/2026-06-06-rush-03.md`). Channel + DI + LogCapture, 5 cas io.
  - ✅ **#4 Grader « projet » (fondation moteur)** — FAIT (PR #12, plan
    `docs/superpowers/plans/2026-06-06-grader-projet.md`). Type `projet` : compile multi-fichiers +
    cas io optionnels + **assertions d'architecture Roslyn** (`requires_types`, `forbidden_dependencies`
    namespace→namespace), trigger `project_structure`. **Constat clé** : la compilation multi-fichiers
    et les deliverables en sous-chemins existaient déjà → la valeur du grader projet = les assertions
    d'archi. Tests **155** verts.
  - ✅ **#5 Module Clean Architecture** — FAIT (PR #13, `content/modules/36-clean-architecture`,
    exo `ex00-couches` en couches Domain/Application/Infrastructure + composition root ; noté `projet`
    io + archi). Outil `try` étendu aux cas `projet`. Plan `.../plans/2026-06-06-module-clean-archi.md`.
  - ✅ **#6 Module Docker** — FAIT (PR #14, `content/modules/37-docker`, lecture : Dockerfile
    multi-étapes, `dotnet publish -t:PublishContainer` sans Dockerfile, images chiseled).
  - ✅ **#8 Module Silk.NET** — FAIT (PR #15, `content/modules/38-silk-net`, lecture : fenêtrage,
    boucle Load/Update/Render, OpenGL clear, entrées ; non déterministe → guidé).
  - ✅ **#3 Harnais réseau (fondation moteur)** — FAIT (PR #16, plan
    `docs/superpowers/plans/2026-06-06-grader-reseau.md`). `NetworkHarness` (écho TCP loopback) +
    grader `reseau` (injecte host/port en args, compare io). Modèle `NetworkConfig`. Tests **161** verts.
    **Suivi** : serveur HTTP (`HttpListener`) + exo pilote M22 + bascule lecture→auto-noté.
  - ✅ **#9 Adoption contenu du grader git** — FAIT (PR #18). Fixture déclarative
    (`GitAssertions.Fixture`/`GitFixtureStep`) + `GitFixtureBuilder` (LibGit2Sharp) ; `ContentValidator`
    valide un exo `git` via sa fixture (pas de `solution/`). **M05 → auto-noté** (`ex00-branche-merge`).
    Tests **164** verts. **Suivi #17** : notation live côté `grade-received` (dépôt bare + signal « tenté »).
  - ✅ **#7 Module Blazor** — FAIT (PR #20, `content/modules/39-blazor`, lecture : composants, paramètres,
    `@bind`, cycle de vie, DI, modèles de rendu .NET 10 ; web/DOM non déterministe → guidé).

  **➡️ BACKLOG V3 D'ORIGINE (issues #1–#9) : TERMINÉE.** Les modules plateforme non gradables par la
  moulinette console (Docker, Silk.NET, Blazor) sont livrés en **lecture guidée**, cohérent avec
  M22/M34. **Enhancements de suivi** (hors backlog d'origine) : **#17** notation live git,
  **#19** harnais web Blazor (rendu DOM), serveur **HTTP**/exo **M22** (cf. #3), rush Clean Arch (optionnel).

## v4 — application desktop Photino (S1 → S8 FAITS ✅, S9 à suivre)
Objectif : **remplacer l'UX recrue console par une app de bureau Photino.Blazor**, **sans toucher au
moteur ni au CLI headless** (`grade-received` tourne dans le hook `post-receive` → reste CLI). v4 =
*remplacer la surface recrue*, pas supprimer le cœur.
- **Spec** : `docs/superpowers/specs/2026-06-06-v4-photino-desktop-design.md` (design validé en brainstorming).
- **Plan S1** : `docs/superpowers/plans/2026-06-06-v4-s1-foundation.md` (bite-sized, prêt à exécuter).
- **Backlog** : milestone **#2 « v4 — application desktop Photino »**, label `v4`, issues **#22–#31**
  (1 sprint = 1 issue, comme V3 ; plan par sprint rédigé au démarrage du sprint).
- **Architecture cible** : `Piscine.Components` (RCL partagée) consommée par `Piscine.Desktop`
  (Photino, livré) **et** `Piscine.DevHost` (Blazor Server, **harnais test/dev non livré**, ex-`Piscine.Web`) ;
  `Piscine.App` (services sans UI : statut git, coaching, check, PtyService) ; moteur + `Piscine.Cli` intacts.
- **Décisions clés** : app **complète** le vrai git (statut, init, **terminal embarqué xterm.js + Pty.Net**,
  **coaching par shim git + état dépôt** — pas de parsing stdout) ; **pas d'éditeur embarqué** (IDE externe) ;
  setup webview **unique toléré** (WebView2/`libwebkit2gtk`, checklist encadrant) ; pyramide de tests
  **xUnit + bUnit + Playwright** sur le DevHost (le shell Photino natif = smoke manuel par OS).
- **Spikes en premier** : S1 (Photino rend la RCL) puis S2 (terminal PTY cross-platform) = risque dominant.

### SCRUM v4 — avancement (1 sprint = 1 issue, branche `v4/sNN-...`, plan → impl → revue agent → docs → retex → PR mergée)
- ✅ **#22 S1 — Fondation** (PR #32 mergée). RCL **`Piscine.Components`** (composants + services
  Markdig migrés de l'ex-`Piscine.Web` + nouveau composant réutilisable `MarkdownView`) consommée par
  **`Piscine.DevHost`** (Blazor Server, ex-`Piscine.Web`, hors release) et **`Piscine.Desktop`**
  (Photino.Blazor 3.2.0, **spike OK** : fenêtre native ouverte, rend un composant RCL, 0 exception).
  **`Piscine.App`** (squelette services, réf. Core/Grading/Git). Pyramide de tests posée : **xUnit** +
  **bUnit 2.7.2** (`Piscine.Components.Tests`) + **Playwright 1.60.0 E2E** (`Piscine.DevHost.E2E`).
  **167 tests verts**, build 0 warning, moteur + `Piscine.Cli` + `release.yml` intacts. **À confirmer
  visuellement par le proprio** : la fenêtre Photino (`dotnet run --project src/Piscine.Desktop -c Release`)
  doit afficher un `<h1>` « Piscine .NET », du gras, un bloc de code C#.
- ✅ **#23 S2 — Spike terminal PTY** (PR #33 mergée). **Risque dominant LEVÉ sur Windows** : `PtyService`
  (`Piscine.App/Terminal/`, **Porta.Pty 1.0.7** — ConPTY/forkpty ; `Pty.Net` du nom de l'issue = abandonné)
  lance un vrai shell ; composant **`Terminal`** (RCL, **xterm.js 6.0.0** UMD vendorisé + module ESM) ; pont
  PTY↔xterm dans la page DevHost `@page "/terminal"` (`@rendermode InteractiveServer`, garde **dev-only**).
  Prouvé bout-en-bout (test PtyService echo + **smoke Playwright** taper→sortie DOM + vérif visuelle : vrai
  prompt `cmd.exe`). **169 tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts. Retex +
  **checklist smoke par OS (action proprio)** : `docs/superpowers/retex/2026-06-06-v4-s2-pty.md`. Limites
  notées : orphelin au drop de circuit SignalR (→ argument Photino), backpressure non coalescée (suivi).
- ✅ **#24 S3 — Moteur statut git + coaching** (PR #34 mergée). `GitStatusService`+`RepoState` (LibGit2Sharp
  lecture seule) + `CoachingService` (**8 règles spec §5**, pur, agnostique shell, jamais de note) dans
  `Piscine.App`. **Shim `git`** (`Piscine.GitShim`, `AssemblyName=git`, hors release) relaie le vrai git
  transparemment + émet `{argv,exitCode,cwd}` sur **named pipe** (PAS de parsing stdout) ; câblé dans
  `PtyService` (PATH + `PISCINE_REAL_GIT` + canal) ; page `/terminal` à cwd **temp isolée** affiche panneau
  statut + cartes. Prouvé : unitaire (1 `[Fact]`/règle) + intégration IPC + **E2E plein-stack** (taper
  `git commit` rien stagé → carte). **195 tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts.
  Retex + checklist OS : `docs/superpowers/retex/2026-06-06-v4-s3-git-coaching.md`.
- ✅ **#25 S4 — `check` instantané in-process** (PR #35 mergée). `CheckService`+`CheckOutcome` (`Piscine.App/Checking/`)
  rejoue la chaîne du moteur (`ContentLocator`→`SubmissionLoader`→`ExerciseGrader.Grade`) **sans console ni
  progression** → résultat structuré UI ; indice/`course_ref` résolus comme `ResultFormatter.MatchHint`.
  Composant **`CheckFeedback`** (RCL : verdict + diff *attendu/obtenu* + carte indice + lien cours) ; page
  **`/check`** (sélecteur d'exo → `CheckService`). DI DevHost via `PiscineLayout` configurable par env
  (`PISCINE_CONTENT`/`PISCINE_WORKSPACE`/`PISCINE_HOME`). Garde : `Grade` **sérialisé** (Console.SetOut global).
  Prouvé : 5 unit (pass/fail+diff/introuvable/vide/déterminisme) + 3 bUnit + **E2E** (exo faux → diff). **204
  tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts. (Le diff est du texte dans
  `GraderResult.Messages` — non structuré ; rendu verbatim, assertions sur `data-testid`/`Trigger`.)
- ✅ **#26 S5 — Navigation d'exercices + progression** (PR #36 mergée). `ProgressService`+modèle (`Piscine.App/Progress/`)
  dérive un **statut par exo** (NonCommencé/EnCours/Commité-non-poussé/Poussé-noté/À revoir) en LISANT `progress.json`
  (`ProgressStore.Load`) + `RepoState` (S3) + présence de livrables — **lecture seule**, champ `Source` + infobulle
  « best-effort » (honnêteté : `progress.json` ne distingue pas check-local de grade-received → `PousseNote` best-effort,
  dégradation verrouillée par test). Composants **`StatusBadge`/`ProgressList`** + page **`/progress`** + liens NavMenu
  (Progression, Vérifier). Prouvé : unit (1/statut, fixtures git) + bUnit (5 statuts) + E2E (`progress.json` planté via
  `ProgressStore.Save`). **219 tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts.
- ✅ **#27 S6 — Lecteur de cours/sujets + parité mode sombre** (PR #37 mergée). Rendu Markdig/pages déjà
  livrés en S1 → S6 ajoute le **sommaire** `CourseToc` (RCL) qui **dérive titres + ancres du HTML rendu par
  Markdig** (ancres == ids émis ; corrige les liens cassés pour titres accentués FR — Markdig retire les
  diacritiques (FormD), pas `CourseAnchors` (FormC)). **Thème partagé** : CSS `[data-theme]` + toggle JS
  migrés de `Piscine.DevHost` vers la RCL (`wwwroot/{css/piscine.css, js/theme.js}`, servis `_content/...`)
  → les DEUX hôtes (DevHost + Photino `index.html`) ont mode sombre + coloration. Prouvé : bUnit (ancres
  accentuées verrouillées) + E2E (sommaire + ancre valide + hljs + bascule `data-theme`) + vérif visuelle
  (0 ancre cassée sur `/module/05-...`). **225 tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts.
  Note : double rendu cours/page (sommaire + corps) négligeable (revue OK).
- ✅ **#28 S7 — Init/setup in-app** (PR #38 mergée). `InitService` (`Piscine.App/Init/`) **enrobe**
  `GitWorkspace.Initialize` (même appel que le CLI `init` — **aucun seam moteur** : déjà `public static` +
  idempotent) → workspace + dépôt **bare** « origin » + hook **`post-receive`**. `Status()` détecte « déjà
  initialisé » (lecture seule). Composant **`InitPanel`** (`/init`) statut + bouton + résultat ; lien NavMenu.
  Chemin de l'exe du hook = **paramètre explicite** (`PISCINE_EXE`, pas `Environment.ProcessPath` sous l'hôte).
  Prouvé : 6 unit (hook=chemin fourni, idempotent) + 3 bUnit + E2E (vierge→init→idempotent + **preuve FS**
  du hook dans un home isolé). **235 tests verts**, build 0 warning, moteur/`Cli`/`release.yml` intacts.
- ✅ **#29 S8 — Surveillance du résultat de push** (PR #39 mergée). `PushResultWatcher` (`Piscine.App/Push/`,
  `FileSystemWatcher` + debounce 250 ms + relecture + snapshot-diff, **lecture seule**) surveille le `progress.json`
  écrit par `grade-received` après un `git push` et publie un événement ; page **`/resultat`** (lien NavMenu)
  **s'auto-rafraîchit** (sans clic), rend le verdict via `StatusBadge` (S5) + lien `/check` (S4). **Constat clé** :
  `grade-received` ne persiste que `progress.json` (statut) ; le diff riche part sur stdout du hook → perdu. → rendu
  **statut-only** + pont `/check` ; **seam « persister le résultat riche » = issue de suivi #40** (NON fait, grader
  inchangé). Prouvé : 6 unit + 4 bUnit + E2E (artefact écrit → rendu sans action). **246 tests verts**, build 0
  warning, moteur/`Cli`/`release.yml` intacts.
- ⏭ **#30 S9** = **packaging/release Photino** (`Piscine.Desktop` livré : publish self-contained par OS, libs
  natives Photino/WebView2/libwebkit2gtk) + **docs setup webview** (checklist encadrant par OS).
- **Suivis notés (pour sprints suivants)** : (a) déplacer `Error.razor` (dépend de `HttpContext`, host-only)
  de la RCL vers `Piscine.DevHost` ; (b) **câbler l'hôte Photino `Piscine.Desktop`** sur les services `Piscine.App`
  (`CourseCatalog`/`GitStatusService`/`CheckService`/`ProgressService` + routage RCL) — aujourd'hui spike, ne monte que
  `MarkdownView` ; (c) confirmer terminal **et** coaching sur Linux/macOS (checklists retex S2/S3) ; (d) coalescer la
  sortie PTY si verbeuse ; (e) **attribution git par exo** dans le statut de progression (préfixe de chemin ; S5 = repo-wide best-effort).

### Pièges v4 (RCL / Blazor / Photino / tests) — appris en S1, réutilisables
- **RCL** (`Microsoft.NET.Sdk.Razor`) : ajouter `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
  et les `using` explicites (le SDK Razor ne porte pas les implicit usings du SDK Web → importer
  `IConfiguration`, `HttpContext`, etc.). `_Imports.razor` à la racine de la RCL.
- **Routage RCL** : un `@page` déplacé dans une RCL n'est découvert côté serveur qu'avec
  `MapRazorComponents<App>().AddAdditionalAssemblies(typeof(<TypeRCL>).Assembly)` dans le host (sinon 404 nu).
- **`@Assets["..."]`** : pour un asset colocalisé d'une RCL, la clé doit être préfixée
  `_content/<AssemblyRCL>/...` ; la clé nue passe à travers, non fingerprintée → **404** (bug S1 attrapé en
  revue sur `ReconnectModal.razor.js`). Le CSS scopé (`.razor.css`) se bundle tout seul
  (`_content/<RCL>/<RCL>.bundle.scp.css`) → aucun souci, seules les clés `@Assets` écrites à la main cassent.
- **Photino.Blazor** : épingler **3.2.0** (sur Photino.NET v3 ; la 4.x plafonne net9). Avec WarningsAsErrors :
  ajouter explicitement `Microsoft.AspNetCore.Components.WebView 10.0.8` (sinon **NU1605**, downgrade
  transitif vs framework net10) et `<Content Update="wwwroot\**">` (pas `Include` — **NETSDK1022**, le SDK
  Razor inclut déjà `wwwroot`). Libs natives par RID dans `runtimes/<rid>/native/` (WebView2 sous Windows ;
  `libwebkit2gtk` à documenter pour Linux en S9). `blazor.webview.js` est servi en mémoire, pas sur disque.
  Fenêtre native non vérifiable par un agent → smoke = « se lance, reste vivant ~12 s, 0 exception ».
- **bUnit 2.x** : base `BunitContext` (pas `TestContext` 1.x), rendu `Render<T>()` (pas `RenderComponent<T>()`) ;
  projet de test en `Sdk="Microsoft.NET.Sdk.Razor"` ; enregistrer les services `@inject` dans `Services`
  avant le rendu.
- **Playwright** : `playwright.ps1 install chromium` ; garder la run solution verte **sans** navigateur (CI
  ubuntu) en skippant (catch `PlaywrightException` sur `LaunchAsync` → `return` ; pas d'`Assert.Skip` en
  xUnit 2.x). Résoudre la racine repo en remontant vers `Piscine.slnx` (jamais le CWD du runner), port dédié,
  poll de disponibilité, tuer l'arbre de processus en `DisposeAsync`.
- **`.gitignore`** : le motif VS `*.e2e` matche un dossier `*.E2E` sur FS insensible à la casse (Windows,
  `core.ignorecase=true`) → exclut silencieusement le projet E2E ; négation pour ré-inclure les sources.

## DÉCISIONS actées (ne pas reposer)
- v1.0 = curriculum complet, blocages branchés. **Packaging M.E.* + EF Core : OUI.**
- Git/réseau (M05/M14/M22) = modules **lecture** ; graders dédiés branchés.
- M13 = io simple + **`mutation`** (`ex03-mutation`) ; grader élève-écrit-tests **livré** (type `mutation`).
- Difficulté = champ `difficulty` + exos **bonus non bloquants** (fait, moteur).
- **Agents d'authoring = `sonnet`.**
- **Tag/release = action publique** → demander le **go** au proprio avant `git tag` + push.

## Pièges connus
- Pousser un tag `v*` déclenche `release.yml` (release publique) — irréversible, demander accord.
- `Piscine.Grading.csproj` a `IsTestProject=false` (sinon `dotnet test` tente d'exécuter la lib).
- Tests Grading/Git : parallélisation xUnit désactivée (Console global / verrous git Windows).
- En dev, définir `PISCINE_CONTENT` vers le `content/` du repo (sinon `bin/.../content` vide).
- Avertissements CRLF de git sous Windows = bénins (`.editorconfig` impose LF).
