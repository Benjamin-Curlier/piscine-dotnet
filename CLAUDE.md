# CLAUDE.md — Piscine .NET

Guide de travail pour les agents (et les humains) sur ce dépôt. **Langue du projet : français**
(cours, sujets, messages, commits, PR). Réponds et commente en français.

## Ce qu'est le projet

Bootcamp d'onboarding façon « piscine » (Epitech/42) pour les fondamentaux **C# (.NET 10)**, avec une
**moulinette auto-correctrice 100 % locale** (Roslyn embarqué, zéro SDK requis côté recrue), un
apprentissage du **vrai git**, et une **distribution self-contained** (Windows + Linux). Deux UX, même
moteur : app de bureau **PhotinoX/Blazor** (`Piscine.Desktop`) et **CLI** `piscine` (`Piscine.Cli`).

Philosophie figée (cf. `docs/wiki/Home.md`) : **retour éducatif, jamais de note** (statuts *Réussi /
À revoir / Non corrigé*) ; **correction par groupe séquentielle, arrêt au premier échec** ; **contenu
data-driven** (ajouter un exercice = déposer des fichiers, sans recompiler) ; **les corrigés
`solution/` ne sont jamais distribués** (exclus par `package-content`, mais servent la gate
`validate-content`).

## Commandes

```bash
dotnet build Piscine.slnx -c Release            # build (TreatWarningsAsErrors=true — voir gotcha)
dotnet test  Piscine.slnx -c Release            # tous les tests (xUnit + bUnit + Playwright E2E)
dotnet format Piscine.slnx                       # DOIT être propre avant toute PR
dotnet run --project src/Piscine.Cli -- <cmd>    # CLI : list|start|check|try|status|init|
                                                 #   grade-received <sha>|validate-content|
                                                 #   package-content <src> <dest>|new exercise <mod> <id>
dotnet run --project src/Piscine.Desktop -c Release   # app de bureau (fenêtre native)
dotnet run --project src/Piscine.DevHost              # site/harnais Blazor de dev → http://localhost:5244
```

Valider le contenu (même commande qu'en CI) :
`PISCINE_CONTENT=$PWD/content dotnet run --project src/Piscine.Cli -- validate-content`

## Architecture (src/)

Moteur & CLI :
- **`Piscine.Core`** — modèles (`Model/`), découverte de contenu (`Content/`), chargement YAML,
  progression (`Progression/`). `PiscineLayout.FromEnvironment()` est le résolveur d'emplacements partagé.
- **`Piscine.Grading`** — cœur de la moulinette. `Graders.Default()` assemble les 7 graders :
  `io`, `norme`, `unit`, `mutation`, `git`, `projet`, `reseau` (voir table plus bas). Compile le code
  recrue avec **Roslyn** (`CompilationService`) et l'exécute via le bac à sable.
- **`Piscine.Sandbox`** (+ **`.Contracts`**, contrat IPC) — exécute le code recrue dans un **processus
  enfant jetable**, tué au timeout, **fail-closed** (jamais de faux « Réussi » si le sandbox manque).
- **`Piscine.Git`** — rendu git (LibGit2Sharp) + commande `grade-received` (notation au push).
- **`Piscine.Cli`** — binaire `piscine` (top-level `Program.cs`, un simple switch de commandes).

App de bureau :
- **`Piscine.Components`** — RCL Blazor partagée (pages/composants + rendu Markdig).
- **`Piscine.App`** — services UI-less (check, statut/coaching git, progression, terminal PTY, init,
  surveillance du push). Pas de dépendance UI ; testé unitairement.
- **`Piscine.Desktop`** — hôte PhotinoX.Blazor livré. **`Piscine.DevHost`** — site Blazor de dev (hors
  release). **`Piscine.GitShim`** — shim `git` pour le coaching au terminal.

Tests : `tests/` (xUnit ; bUnit pour les composants ; Playwright pour l'E2E DevHost).

### Les 7 graders (déclarés par exercice dans `manifest.yaml`)

| Type | Rôle | Bloquant |
|---|---|---|
| `io` | Compile en exe, lance dans le sandbox (stdin/args/timeout), compare stdout/exit | oui |
| `unit` | Compile code recrue + tests xUnit cachés (`grader/`), exécute par réflexion | oui |
| `norme` | Diagnostics de style Roslyn (Formatter + `.editorconfig`) | non (avertissement) |
| `mutation` | La recrue écrit ses tests ; confrontés à une impl. de réf. + mutants nommés | oui |
| `git` | Vérifie l'état du dépôt rendu (branches, commits, fusions, contenu) via LibGit2Sharp | oui |
| `projet` | Compil. multi-fichiers + `io` optionnel + assertions d'archi (`forbidden_dependencies`) | oui |
| `reseau` | Harnais d'écho TCP loopback, injecte host/port, compare `io` | oui |

## Contenu (`content/`)

`modules/<NN-nom>/exercises/<exNN-id>/` avec `manifest.yaml`, `subject.md`, `starter/`, `solution/`
(+ `grader/` pour `unit`/`mutation`). L'ordre des exercices d'un groupe est dans `module.yaml`.
`validate-content` exige que **chaque `solution/` passe ses propres graders** — c'est le garde-fou qui
empêche de livrer un exercice cassé. Ne jamais committer un exercice qui ne passe pas `validate-content`.
`starter/` et `solution/` doivent produire la même sortie ? Non : `starter/` est le squelette remis,
`solution/` le corrigé de référence (jamais distribué).

## Conventions

- **Conventional Commits en français** : `feat(grading): …`, `fix(cli): …`, `docs: …`, `chore(ci): …`.
- `main` est protégée : PR avec **CI verte** (`build-test` + `validate-content` + **CodeQL**) et **≥ 1
  revue approuvée**. Mettre à jour `CHANGELOG.md` quand le changement touche l'utilisateur.
- `Directory.Build.props` impose `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`**
  pour tout le dépôt : le moindre avertissement d'analyseur casse le build. Reste au vert.
- Variables d'environnement clés : `PISCINE_CONTENT`, `PISCINE_HOME`, `PISCINE_WORKSPACE`,
  `PISCINE_SANDBOX`, `PISCINE_REAL_GIT` (voir `PiscineLayout` / `PiscinePaths`).

## Gotchas

- **Build cassé sur clone frais (`NU1903`)** : `TreatWarningsAsErrors=true` promeut l'audit NuGet en
  erreur. `src/Piscine.Cli` référence `Microsoft.EntityFrameworkCore.Sqlite`, qui traîne le paquet natif
  vulnérable `SQLitePCLRaw.lib.e_sqlite3` (GHSA-2m69-gcr7-jv3q) → `dotnet build` échoue. Contournement
  d'observation : ajouter `-p:NuGetAudit=false`. **Attention** : ces 5 `PackageReference` du CLI
  (`EntityFrameworkCore.Sqlite`, `Extensions.DependencyInjection/Hosting/Logging/Logging.Console`) NE sont
  **PAS** inutilisées bien que `Program.cs` ne les appelle pas : elles peuplent le `TRUSTED_PLATFORM_ASSEMBLIES`
  du process, et `CompilationService` s'en sert comme jeu de références Roslyn pour **compiler le code recrue**
  des modules `18-injection-dependances`, `19-logging`, `20-generic-host`, `35-ef-core`. Les retirer casse
  la correction de ces modules + `validate-content`. Le vrai correctif du build est de traiter le paquet
  transitif vulnérable (bump EF Core / référence directe d'un `SQLitePCLRaw` corrigé / politique d'audit),
  **pas** de supprimer les dépendances. Ne masque pas l'audit dans le dépôt.
- **Divergence de correction CLI ↔ Desktop (à vérifier/corriger)** : seul `Piscine.Cli` porte le paquet
  EF Core. Le Desktop corrige **en process** (`Graders.Default()` via `CheckService`) et ne référence pas
  EF Core (DI/Logging/Hosting arrivent transitivement par Blazor, mais pas EF Core) → le module `35-ef-core`
  risque de compiler côté CLI/`validate-content`/CI mais d'échouer dans « Vérifier » du bureau. Vérifier et,
  au besoin, faire porter les mêmes assemblies de référence à l'hôte de correction bureau.
- **Self-contained, pas single-file** : la résolution des références Roslyn depuis
  `TRUSTED_PLATFORM_ASSEMBLIES` casse en single-file — garder `PublishSingleFile=false`.
- **Sandbox fail-closed** : ne jamais court-circuiter le processus enfant ; un sandbox indisponible doit
  produire *Non corrigé*, jamais *Réussi*.
- Ne jamais faire fuiter `solution/` dans le paquet distribué ni dans les logs recrue.

## Vérification locale

Avant de committer un changement moteur : `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes`
+ `validate-content`. La skill **`piscine-verify`** enchaîne ces étapes.
