# HANDOFF — reprise à froid

> Point d'entrée pour reprendre le projet **sans contexte préalable**. Dernière MAJ : 2026-06-09.
> Lis aussi `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md` (design complet) et
> `docs/wiki/Curriculum.md` (état du curriculum).

## C'est quoi
Bootcamp « piscine » façon Epitech/42 pour **C# / .NET 10**. Livrable = zip autonome (cours +
exercices + appli console qui sert d'UX recrue ET de **moulinette auto-correctrice** locale, sans
SDK). Rendu via **vrai git** (dépôt bare local + hook `post-receive`). Correction **par groupe,
séquentielle, stop au 1er KO** ; **retour éducatif, jamais de note**. Exercices **data-driven**
(`manifest.yaml`) → ajout sans recompiler.

Repo : https://github.com/Benjamin-Curlier/piscine-dotnet (**public** depuis le 7 juin 2026). Solution **`Piscine.slnx`** (.NET 10).
Branche par défaut **`main`**. Commits conventionnels en **français**, terminés par
`Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## État actuel (résumé)
- **🔒 Release `v3.1.1` (sprint 2026-06-09, branche `fix/review-hardening-followups`)** : version
  corrective. **Isolation de l'exécution du code recrue dans un processus enfant jetable** (nouveau
  projet `Piscine.Sandbox`) — boucle infinie **réellement terminable** (kill de l'arbre au timeout),
  fin des fuites thread/assembly et de la corruption inter-exécutions, fixtures disposées, **fail-closed**
  si le bac à sable manque. + **CSP** défense-en-profondeur Desktop. + durcissements de notation (#58 :
  fail-closed type/cas manquants, progress.json corrompu, traversal git, hook ref vide, XSS markdown ;
  GitGrader HeadRef ; validation stricte des clés manifest). 305 tests verts, `validate-content` OK
  (dev + artefact publié). Spec/plan/retex : `2026-06-09-grading-sandbox-isolation*`.
- **🔧 Migration PhotinoX (sprint 2026-06-08, branche `feat/photinox-migration`)** : `Piscine.Desktop`
  passe de `Photino.Blazor 3.2.0` à **`PhotinoX.Blazor 4.2.0`** (fork net10-natif). Épingle WebView
  NU1605 **supprimée** ; libs natives **`PhotinoX.Native.{dll,so}`** ; Linux **webkit2gtk-4.1**
  (runner `ubuntu-24.04`). API/namespace `Photino.Blazor` conservés → **0 changement de code applicatif**
  (1 ligne : fallback non-null sur `ShowMessage`). Spec : `specs/2026-06-08-photinox-migration-design.md` ;
  plan : `plans/2026-06-08-photinox-migration.md` ; ADR : `adr/2026-06-08-photinox-fork.md`. Cible release **v3.1.0**.
  **⚠️ AppImage *offline* abandonnée** (WebKitGTK release ignore `WEBKIT_EXEC_PATH` → pas de webkit
  embarqué fonctionnel ; cf. ADR) : seule l'**AppImage online** est publiée → **5 artefacts** (zips win/linux,
  installeurs Windows offline/online, AppImage Linux online). `ci.yml` : `appimage-offline-dryrun` →
  `appimage-online-dryrun` (garde de build, plus de test webkit-less).
- **🏷️ Release `v3.0.0` PUBLIÉE** (7 juin 2026, tag `v3.0.0` sur `ffe2232`, `release.yml` vert au **1ᵉʳ run
  réel des installeurs**). 6 artefacts attachés : installeurs Windows `.exe` (offline 335 Mo / online 135 Mo),
  AppImages Linux (offline 141 Mo / online 55 Mo), zips win/linux. Bundle = app de bureau Photino (cours/
  check/progress/init/**terminal+coaching**/résultat riche) + **notation live git** (#17) + moteur v2 compatible.
  CHANGELOG `[v3.0.0]` ; release : <https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v3.0.0>.
- **⚠️ Le dépôt est désormais PUBLIC** (décision proprio du 7 juin 2026, pour activer l'onglet **Wiki** GitHub,
  qui exige un repo public). **Conséquence assumée** : les **120 dossiers `solution/` (corrigés) sont
  publiquement visibles** — la « décision actée : la recrue ne reçoit jamais les corrigés » ne tient plus que
  pour le **paquet distribué** (`package-content` les exclut toujours du zip/installeur), pas pour le repo.
  Le **Wiki GitHub** est publié (7 pages, source = `docs/wiki/`) : <https://github.com/Benjamin-Curlier/piscine-dotnet/wiki>.
- *(Historique)* **Release `v2.0.0`** (tag poussé, `release.yml` vert, 3 zips win/linux/osx). Bundle
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
- **Tests** : 267 verts (`dotnet test Piscine.slnx -c Release` : Core 46 + App 57 + Components 25 + Git 12 +
  DevHost.E2E 9 + Grading 118). CI GitHub Actions verte sur `main`. (164 avant v4 ; +83 v4 S1→S9b ;
  +6 v5 S11 résultat riche ; +4 v5 S12 = ShimLocator 2 + TerminalPolicy 2 ; **+10 #17 notation live git** =
  Grading 7 + Git 3.) **Linux (PTY/coaching/shim) prouvé via Docker `dotnet/sdk:10.0`** (57/57 App tests) en S12.
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
    Tests **164** verts. ✅ **Suivi #17 FAIT** (PR #52) : notation live au push côté `grade-received` (dépôt
    bare via `HeadRef`, signal « tenté » `attempt`, M05 noté en live, `git push origin --all`).
  - ✅ **#7 Module Blazor** — FAIT (PR #20, `content/modules/39-blazor`, lecture : composants, paramètres,
    `@bind`, cycle de vie, DI, modèles de rendu .NET 10 ; web/DOM non déterministe → guidé).

  **➡️ BACKLOG V3 D'ORIGINE (issues #1–#9) : TERMINÉE.** Les modules plateforme non gradables par la
  moulinette console (Docker, Silk.NET, Blazor) sont livrés en **lecture guidée**, cohérent avec
  M22/M34. **Enhancements de suivi** (hors backlog d'origine) : ✅ **#17 notation live git FAIT** (PR #52) ;
  ⏸ **#19 harnais web Blazor DIFFÉRÉ** (décision proprio, ADR `docs/superpowers/adr/2026-06-07-harnais-web-blazor.md` :
  coût/risque élevés — Razor runtime + ASP.NET Components dans le binaire — pour valeur étroite ; M39 reste
  guidé ; chemin minimal viable documenté) ; restants : serveur **HTTP**/exo **M22** (cf. #3), rush Clean Arch (optionnel).

## v5 — finalisation desktop (terminal in-app, résultat riche, packaging) — **TERMINÉ ✅ (milestone #3 CLOS)**
Milestone **#3** (label `v5`). Objectif : compléter l'app de bureau (terminal embarqué + coaching git
**dans** Photino, packager `Piscine.GitShim`), résultat de push **riche** (#40), confirmation Linux/macOS,
et **révision packaging (zip vs installeur)**. Le proprio autorise **WSL/Docker** pour les tests Linux et
le **2ᵉ écran/souris** pour Photino. **Aucun tag** (release = action proprio). **S11→S14 FAITS** (#40, #45,
#46, #47) ; CHANGELOG « Non publié » prêt ; reste proprio = smoke pré-release installeurs + tag/release.
- ✅ **#40 S11 — `grade-received` persiste le résultat riche** (PR #48 mergée). `grade-received` écrit
  `last-push-result.json` (par exo : statut + diff verbatim + indice + `course_ref`) en plus de
  `progress.json`, **sans changer la logique de notation ni le stdout**. `IPushResultWatcher.LatestRichResult()`
  le lit à la demande ; **`/resultat` rend `CheckFeedback`** (diff/indice/cours) inline, **rétro-compat**
  statut-only (StatusBadge + lien `/check`) si absent. DTO neutre (`PushResultDocument`) dans `Piscine.Core`
  (sens des deps). **253 tests** (+6), Grading/CLI/CI intacts. Retex : `docs/superpowers/retex/2026-06-07-v5-s11-rich-push-result.md`.
- ✅ **#45 S12 — Terminal + coaching git dans Photino** (PR #49 mergée). Terminal embarqué + coaching
  **fonctionnels dans l'app packagée** : `GitShim` publié dans `desktop/gitshim/` (release.yml) ; garde
  `IHostEnvironment` remplacée par **`TerminalPolicy`** injectable (Photino=true, DevHost=Development) ;
  résolution shim **hôte-agnostique** (`ShimLocator` : packagé `<exe>/gitshim` puis dev `Piscine.slnx`) ;
  DI Photino (Pty/Coaching/Channel) + lien NavMenu `/terminal`. **Linux PROUVÉ via Docker** (`dotnet/sdk:10.0`,
  57/57 App tests sur WSL2 : PTY forkpty + coaching named-pipe/socket Unix + shim) → **lève les checklists
  proprio S2/S3**. 257 tests, Grading/CLI/Core intacts. Retex : `docs/superpowers/retex/2026-06-07-v5-s12-terminal-photino.md`.
  **Reste proprio** : confirmer visuellement terminal+coaching dans la fenêtre native (Win/Linux/macOS).
- ✅ **#46 S13 — Installeurs Windows + Linux (offline & online), macOS abandonné** (PR #50 mergée).
  **Décision proprio** : 2 modes (offline/online) × 2 OS ; **macOS abandonné** (`osx-arm64` retiré) ; zips
  conservés. **Linux AppImage** (`build/installer/linux/`) : offline = webkit2gtk-4.0 + gtk + git bundlés
  (linuxdeploy + plugin gtk, **bâti sur ubuntu-22.04**), **hors-ligne PROUVÉ en CI** (docker `--network=none`
  + xvfb, conteneur sans webkit → charge `app://localhost/`) ; online = webkit système. **Windows Inno**
  (`build/installer/windows/piscine.iss`) : per-utilisateur (`PrivilegesRequired=lowest`), offline = runtime
  **WebView2 Standalone Evergreen** embarqué (run-if-missing) / online = bootstrapper ; **vérifié en local**
  (install/désinstall) **+ compile en CI**. `release.yml` = 3 jobs (package-linux / package-windows / release) ;
  `ci.yml` = dry-runs AppImage-offline (hors-ligne) + Windows-installeur (compile). **Aucun `src/` touché, aucun
  tag.** **CONSTAT clé (v3.0.0) : Photino.Blazor 3.2.0 → webkit2gtk-4.0 (PAS 4.1)** → online Linux prereq `libwebkit2gtk-4.0-37`.
  **⤷ MAJ v3.1.0 : migration PhotinoX.Blazor 4.2.0 → webkit2gtk-4.1, runner `ubuntu-24.04`, prereq `libwebkit2gtk-4.1-0`.**
  Retex : `docs/superpowers/retex/2026-06-07-v5-s13-installers.md`. WebView2 fwlinks : offline `linkid=2124701`, online `LinkId=2124703`.
- ✅ **#47 S14 — Docs recrue + déploiement** (PR #51 mergée). **Dernier sprint v5.** `mise-en-oeuvre.md`
  (réécrit) + `deploiement.md` reflètent l'état final : **terminal embarqué + coaching git** (page *Terminal*,
  `git push` du rendu possible au terminal de l'app ou système), **résultat de push riche** (*Vérifier*/*Résultat*
  = diff/indice/lien cours), **installeurs** Windows `.exe` per-utilisateur + Linux `.AppImage` (offline/online)
  + **zips** conservés, **macOS abandonné**, **webkit2gtk-4.0** (pas 4.1). `deploiement.md` §4 = 3 jobs release ;
  §5 dry-runs + smoke MAJ. `CHANGELOG.md` « Non publié » : terminal+coaching+résultat riche **passés en Ajouté**.
  `Curriculum.md` + `docs/wiki/Mise-en-oeuvre.md` alignés. **Docs/CHANGELOG uniquement** (0 `src/`/`.github`),
  `validate-content` OK, **aucun tag**. Retex : `docs/superpowers/retex/2026-06-07-v5-s14-docs.md`.

  **➡️ BACKLOG v5 (#40/#45/#46/#47) TERMINÉ, milestone #3 CLOS** : app de bureau Photino complète
  (terminal+coaching in-app, résultat riche, installeurs Win+Linux offline/online), docs alignées ;
  moteur/CLI/`grade-received` inchangés ; **aucun tag** (release = décision proprio ; CHANGELOG « Non publié »
  prêt). **Reste proprio** : smoke pré-release des installeurs (fenêtre native + terminal/coaching, Win+Linux).
  **Enhancements de suivi** (hors v5, milestone #1) : ✅ **#17 notation live git FAIT** (PR #52, 267 tests) ;
  ⏸ **#19 harnais web Blazor DIFFÉRÉ** (décision proprio + ADR `2026-06-07-harnais-web-blazor.md` ; M39 reste guidé).
  **➡️ BACKLOG « finish s14 + backlogs » TERMINÉ** : S14 (#47) + #17 livrés/mergés ; #19 tranché (différé).

## v4 — application desktop Photino (S1 → S10 FAITS ✅ — **milestone #2 CLOS**, backlog #22–#31 terminé)
Objectif : **remplacer l'UX recrue console par une app de bureau Photino.Blazor**, **sans toucher au
moteur ni au CLI headless** (`grade-received` tourne dans le hook `post-receive` → reste CLI). v4 =
*remplacer la surface recrue*, pas supprimer le cœur. **ÉTAT : v4 livré côté capacité** — app de bureau
fonctionnelle (cours/check/progress/init/resultat), packagée par OS, documentée. **Aucun tag poussé**
(release/pré-release = décision proprio). **Suite = hors backlog v4** (voir « Suivis (post-v4) » + #40).
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
- ✅ **#30 S9 — Packaging/release Photino + docs setup webview** (PR #41 mergée). `release.yml` publie
  **`Piscine.Desktop` self-contained par RID** dans `desktop/` (à côté du CLI `piscine` intact, du
  `content/` et de MinGit Windows) + lanceurs `start-piscine-desktop.{cmd,sh}` ; `Piscine.DevHost` jamais
  empaqueté. **Dry-run CI 3 RID** (`ci.yml`) publie + asserte les libs natives **à chaque PR** (filet
  avant tag). Docs : setup webview par OS (WebView2 / `libwebkit2gtk-4.1` / WKWebView) + app desktop dans
  le zip + **checklist smoke pré-release par OS** (action proprio). **Constats clés** : (1) `WinExe`
  publie **cross-RID** sans modif csproj (flag PE ignoré hors Windows) → **aucun fichier `src/` touché** ;
  (2) **CORRECTION** : les libs natives self-contained sont **à la RACINE** du dossier de sortie
  (`PhotinoX.Native.dll/.so/.dylib` depuis v3.1.0 — `Photino.Native.*` en v3.0.0 —, `WebView2Loader.dll`), **pas** `runtimes/<rid>/native/`. Prouvé :
  publish réel des 3 RID + dry-run CI vert (OK linux/win/osx). **246 tests verts**, build 0 warning, moteur
  source intact, **aucun tag** (release = action proprio). Retex : `docs/superpowers/retex/2026-06-06-v4-s9-packaging-release.md`.
- ✅ **#42 S9b — Câbler l'hôte Photino sur les services App** (PR #43 mergée). **Constat déclencheur** :
  S9 avait empaqueté un `Piscine.Desktop` **encore au stade spike S1** (`App.razor` = `MarkdownView`
  statique, pas de Router, aucun service) → le but de v4 (desktop remplace la console) n'était pas atteint
  et S10 ne pouvait pas décrire un flux desktop honnête. S9b monte le **Router de la RCL** + le **flux
  recrue** (`/` cours, `/module`, `/check`, `/progress`, `/init`, `/resultat`) + les services
  `Piscine.App` (port de `DevHost/Program.cs`, sans le terminal). **Risque dominant levé** : render modes
  en hôte WebView → **indirection `InteractiveRenderSettings`** (motif Microsoft Blazor Hybrid) : les pages
  RCL gardent `@rendermode InteractiveServer` mais le symbole résout vers une propriété statique
  (= framework par défaut → DevHost inchangé) que l'hôte Photino annule via
  `ConfigureBlazorHybridRenderModes()` (→ `@rendermode null` → rendu in-process). **NB** : 1ʳᵉ approche
  (render mode global DevHost) **rejetée** car elle cassait la coloration highlight.js des pages cours
  statiques (E2E reader KO → attrapé). **Terminal/coaching déféré** (dépend de `Piscine.GitShim` hors
  release + `IHostEnvironment` ; NavMenu ne lie pas `/terminal` → inatteignable dans Photino). Content path :
  `ContentRootResolver` remonte depuis `AppContext.BaseDirectory` → l'exe en `desktop/` trouve `../content`
  du zip sans `PISCINE_CONTENT`. Prouvé : build 0 warning, **247 tests verts** (DevHost E2E inclus, +1 test
  d'indirection), smoke Photino Windows (fenêtre + `http://localhost/`, 0 crash), moteur/`Cli`/`release.yml`
  intacts, aucun tag. Retex : `docs/superpowers/retex/2026-06-07-v4-s9b-photino-wiring.md`.
- ✅ **#31 S10 — Docs recrue/encadrant + Curriculum/CHANGELOG** (PR #44 mergée). **Dernier sprint du
  backlog v4.** `mise-en-oeuvre.md` : app de bureau = **UX recrue principale** (cours/check/progress/init/
  resultat ; `git push` au **terminal système** ; CLI conservé) — **mention erronée du « terminal intégré »
  retirée** (déféré). `CHANGELOG.md` : section **`## [Non publié]`** (app Photino v4, moteur/CLI/`grade-received`
  inchangés, terminal différé) → prête à versionner. `Curriculum.md` : note UX. `deploiement.md` : smoke
  « la fenêtre **route le flux** ». **Docs/CHANGELOG uniquement** (0 `src/`/`.github`), `validate-content`
  OK, **aucun tag**. Retex : `docs/superpowers/retex/2026-06-07-v4-s10-docs-desktop.md`.
- **Suivis notés (post-v4)** : (a) déplacer `Error.razor` (dépend de `HttpContext`, host-only) de la RCL vers
  `Piscine.DevHost` ; (b) **terminal + coaching dans Photino** (packager `Piscine.GitShim` + fournir
  `IHostEnvironment`/garde adaptée + PTY in-process) — déféré en S9b ; (c) confirmer terminal/coaching/fenêtre
  native sur Linux/macOS (checklists retex S2/S3/S9/S9b) ; (d) coalescer la sortie PTY si verbeuse ;
  (e) **attribution git par exo** dans la progression (préfixe de chemin ; S5 = repo-wide best-effort).

### Pièges v4 (RCL / Blazor / Photino / tests) — appris en S1, réutilisables
- **RCL** (`Microsoft.NET.Sdk.Razor`) : ajouter `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
  et les `using` explicites (le SDK Razor ne porte pas les implicit usings du SDK Web → importer
  `IConfiguration`, `HttpContext`, etc.). `_Imports.razor` à la racine de la RCL.
- **Routage RCL** : un `@page` déplacé dans une RCL n'est découvert côté serveur qu'avec
  `MapRazorComponents<App>().AddAdditionalAssemblies(typeof(<TypeRCL>).Assembly)` dans le host (sinon 404 nu).
  Côté **Photino** (S9b) : `App.razor` = `<Router AppAssembly=... AdditionalAssemblies="new[]{typeof(<TypeRCL>).Assembly}"
  NotFoundPage="typeof(<PageRCL>)">` + un `_Imports.razor` propre à l'hôte (sinon `Router`/`Found`/`FocusOnNavigate` = RZ10012).
- **Render modes RCL partagée entre Web App (DevHost) et BlazorWebView (Photino)** (S9b) : motif Microsoft
  Blazor Hybrid = indirection `InteractiveRenderSettings` (pages gardent `@rendermode InteractiveServer` ;
  `@using static <RCL>.InteractiveRenderSettings` ; l'hôte WebView appelle `ConfigureBlazorHybridRenderModes()`
  → null → in-process). **NE PAS** globaliser le render mode côté Web App (casse le rendu statique + le
  timing highlight.js des pages cours). Détails → [[piscine-v4-blazor-photino-gotchas]].
- **`@Assets["..."]`** : pour un asset colocalisé d'une RCL, la clé doit être préfixée
  `_content/<AssemblyRCL>/...` ; la clé nue passe à travers, non fingerprintée → **404** (bug S1 attrapé en
  revue sur `ReconnectModal.razor.js`). Le CSS scopé (`.razor.css`) se bundle tout seul
  (`_content/<RCL>/<RCL>.bundle.scp.css`) → aucun souci, seules les clés `@Assets` écrites à la main cassent.
- **Photino.Blazor** : épingler **3.2.0** (sur Photino.NET v3 ; la 4.x plafonne net9). Avec WarningsAsErrors :
  ajouter explicitement `Microsoft.AspNetCore.Components.WebView 10.0.8` (sinon **NU1605**, downgrade
  transitif vs framework net10) et `<Content Update="wwwroot\**">` (pas `Include` — **NETSDK1022**, le SDK
  Razor inclut déjà `wwwroot`). **Libs natives à la RACINE de la sortie d'un publish self-contained**
  (`PhotinoX.Native.dll/.so/.dylib` v3.1.0 / `Photino.Native.*` v3.0.0, + `WebView2Loader.dll` Windows), **PAS** sous `runtimes/<rid>/native/`
  (corrigé en S9 par publish réel des 3 RID). `WinExe` publie **cross-RID** (flag PE ignoré hors Windows).
  Runtime webview par OS : WebView2 (Windows, éditions N → Evergreen) / `libwebkit2gtk-4.1` (Linux) /
  WKWebView (macOS intégré) — cf. `docs/deploiement.md`. `blazor.webview.js` est servi en mémoire, pas sur disque.
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
