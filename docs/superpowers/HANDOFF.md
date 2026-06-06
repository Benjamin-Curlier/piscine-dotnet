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
- **Release `v1.0.0` publiée** (tag poussé, `release.yml` vert, 3 zips win/linux/osx attachés).
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
- **Contenu** : modules **M00–M35** + **M36 (Clean Architecture, V3)** + **Rushes 0/1/2/3**.
  - Auto-notés `io` : M00–M04, M06–M13, **M15–M21**, M23, M24–M33, M35.
  - Auto-noté `projet` (V3) : **M36** (`ex00-couches`).
  - Lecture guidée (cours seul, groupe `exercises: []`) : **M05, M14, M22, M34**.
  - **M19 (Logging) & M20 (Generic Host) DÉBLOQUÉS** (post-v1.0, en `io`) — cf. ci-dessous.
- **Site du cours** : `src/Piscine.Web` (Blazor .NET 10, type Docusaurus + **mode sombre**) présente
  cours+sujets dans le navigateur. `dotnet run --project src/Piscine.Web` → http://localhost:5244.
  Dans la slnx (buildé par la CI), **hors release**.
- **Tests** : 131 verts (`dotnet test Piscine.slnx -c Release` : Core 46 + Git 7 + Grading 78). CI GitHub Actions verte sur `main`.
- Arbre propre, tout poussé. Dernier commit `main` : voir `git log -1`.

## Architecture moteur (l'essentiel)
- `src/Piscine.Core` : modèles + découverte de contenu (logique pure, testée).
- `src/Piscine.Grading` : `CompilationService` (Roslyn), graders, `GroupGrader` (stop au 1er KO ;
  un exo `bonus` à revoir ne bloque PAS la suite), `ContentValidator` (gate).
- `src/Piscine.Git` : rendu git (LibGit2Sharp), `grade-received`.
- `src/Piscine.Cli` : orchestration/affichage (`AssemblyName=piscine`). Commandes :
  `list`, `start <exo>`, `check <exo>`, `try <exo>`, `status`, `init`, `grade-received <sha>`,
  `validate-content`, `package-content <src> <dest>`, `new exercise <module> <id>`.
- `src/Piscine.Web` : site du cours (Blazor SSR). Réutilise les loaders de `Piscine.Core`
  (`Services/CourseCatalog`), rend le markdown via Markdig, coloration highlight.js, mode sombre.
  N'est PAS dans le binaire `piscine` packagé.

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
  - ⏳ **#9** adoption contenu du grader git (M05/M14 + `grade-received`).
  - ⏳ **#3** harnais réseau (M22) ; **#6** Docker ; **#7** Blazor (harnais web sur grader projet) ;
    **#8** Silk.NET. (Rush dédié Clean Arch : optionnel, non bloquant.)

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
