# HANDOFF — reprise à froid

> Point d'entrée pour reprendre le projet **sans contexte préalable**. Dernière MAJ : 2026-05-31.
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
- **Moteur** : graders `io` / `unit` / `norme` (Roslyn embarqué, refs via `TRUSTED_PLATFORM_ASSEMBLIES`),
  rendu git, scaffolder `piscine new exercise`, gate `validate-content`. Binaire embarque
  `Microsoft.Extensions.*` (DI/Logging/Hosting) **et** `Microsoft.EntityFrameworkCore.Sqlite`.
- **Contenu** : modules **M00–M28** (sauf **M19/M20** = sur branche `v1.0-blockers`) + **Rushes 0/1/2**.
  - Auto-notés `io` : M00–M04, M06–M13, M15–M18, M21, M23, M24–M28.
  - Lecture guidée (cours seul, groupe `exercises: []`) : **M05, M14, M22**.
- **Tests** : 78 verts (`dotnet test Piscine.slnx -c Release`). CI GitHub Actions verte sur `main`.
- Arbre propre, tout poussé. Dernier commit `main` : voir `git log -1`.

## Architecture moteur (l'essentiel)
- `src/Piscine.Core` : modèles + découverte de contenu (logique pure, testée).
- `src/Piscine.Grading` : `CompilationService` (Roslyn), graders, `GroupGrader` (stop au 1er KO ;
  un exo `bonus` à revoir ne bloque PAS la suite), `ContentValidator` (gate).
- `src/Piscine.Git` : rendu git (LibGit2Sharp), `grade-received`.
- `src/Piscine.Cli` : orchestration/affichage (`AssemblyName=piscine`). Commandes :
  `list`, `start <exo>`, `check <exo>`, `try <exo>`, `status`, `init`, `grade-received <sha>`,
  `validate-content`, `package-content <src> <dest>`, `new exercise <module> <id>`.

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
**v2 — approfondissement C#/.NET (en cours, numérotation M24+).** Reste à créer (io) :
- **M30** Design patterns suite (Decorator, Adapter, Command, Builder, Singleton).
- **M31** Smelly code & refactoring (livrer une version refactorée au comportement identique).
- **M32** Garbage collection (io quand possible : Dispose/`using`, sinon lecture).
- **M33** Discriminated unions (hiérarchie scellée `abstract record` + `sealed`).
- **M34** Interopérabilité (P/Invoke `LibraryImport` simple ; reste en lecture si non portable).
- **M35** Entity Framework Core (DbContext + SQLite in-memory + requêtes `ORDER BY` déterministes).
- Puis **débloquer M19/M20** (cf. ci-dessous) et **enrichir les modules existants** avec des exos bonus.
- Penser à **MAJ `docs/wiki/Curriculum.md`** au fil des nouveaux modules.

**Branche `v1.0-blockers`** (non mergée) — `docs/superpowers/BLOCKERS-v1.0.md` détaille les chantiers
moteur à reprendre, avec cause/résolution/effort :
- M19 Logging & M20 Host (capture du provider console qui contourne `Console.SetOut`).
- Grader **git** dédié (M05/M14, via LibGit2Sharp), **harnais réseau** (M22), grader
  **« élève-écrit-tests »** (M13, mutants), **Rush 3** (worker complet).

**v3 — plateformes & architecture** (nouveau **grader « projet »** multi-fichiers/web) :
Blazor, Silk.NET, Docker, clean architecture. Voir `docs/superpowers/plans/2026-05-31-roadmap-v2-v3.md`.

## DÉCISIONS actées (ne pas reposer)
- v1.0 = curriculum complet, blocages branchés. **Packaging M.E.* + EF Core : OUI.**
- Git/réseau (M05/M14/M22) = modules **lecture** ; graders dédiés branchés.
- M13 = io simple ; grader élève-écrit-tests branché.
- Difficulté = champ `difficulty` + exos **bonus non bloquants** (fait, moteur).
- **Agents d'authoring = `sonnet`.**
- **Tag/release = action publique** → demander le **go** au proprio avant `git tag` + push.

## Pièges connus
- Pousser un tag `v*` déclenche `release.yml` (release publique) — irréversible, demander accord.
- `Piscine.Grading.csproj` a `IsTestProject=false` (sinon `dotnet test` tente d'exécuter la lib).
- Tests Grading/Git : parallélisation xUnit désactivée (Console global / verrous git Windows).
- En dev, définir `PISCINE_CONTENT` vers le `content/` du repo (sinon `bin/.../content` vide).
- Avertissements CRLF de git sous Windows = bénins (`.editorconfig` impose LF).
