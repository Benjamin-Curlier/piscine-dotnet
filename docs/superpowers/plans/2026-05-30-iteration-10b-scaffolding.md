# Itération 10b — Outil `piscine new exercise` (scaffolding) — Implementation Plan

> **For agentic workers:** itération **outil/code** (TDD). Intercalée avant de continuer le contenu, pour accélérer la création d'exercices.

**Goal:** Ajouter la commande `piscine new exercise <module> <id>` qui génère le squelette d'un exercice `io` (manifest pré-rempli, subject, starter/, solution/) dans un module existant, avec un nom de livrable déduit de l'id.

**Architecture / approche :** `ExerciseScaffolder` (Core, statique, comme `StarterInstaller`/`ContentPackager`). Déduction PascalCase du livrable depuis l'id (préfixe `exNN-` retiré). CLI : nouvelle branche `case "new"`. Pas d'édition automatique de `module.yaml` (choix du groupe ambigu → guidance imprimée).

**Tech Stack:** C# / .NET 10, xUnit.

---

## Tasks

- [x] **TDD red** : `tests/Piscine.Core.Tests/ExerciseScaffolderTests.cs` — `Create` génère le squelette ; `DeliverableFileName` (Theory : `ex00-somme`→`Somme.cs`, `ex02-somme-n`→`SommeN.cs`, `ex10-compte-rebours`→`CompteRebours.cs`, `hello`→`Hello.cs`) ; `Create` lève `IOException` si l'exercice existe, `DirectoryNotFoundException` si le module est absent.
- [x] **Green** : `src/Piscine.Core/Content/ExerciseScaffolder.cs` — `DeliverableFileName(id)`, `Create(modulesRoot, moduleId, exerciseId) → exerciseDir`. Templates manifest `io` (TODO), subject, starter, solution.
- [x] **CLI** : `case "new"` → `New(layout, args)` (valide `args[1]=="exercise"` + module + id), `layout.Content.ModulesDirectory`, capture `DirectoryNotFoundException`/`IOException` → exit 2, imprime les étapes suivantes. Aide mise à jour.
- [x] **Doc** : `docs/wiki/Ajouter-un-exercice.md` — étape 1 = `piscine new exercise`.
- [x] **Vérif** : `dotnet test Piscine.slnx -c Release` → 73 verts (Core 29 + Git 6 + Grading 38). Smoke `new exercise 02-boucles ex99-smoke-test` (créé `SmokeTest.cs`, arbo OK, supprimé). `validate-content` → « Contenu valide. ».
- [ ] **Commit** `feat(cli): commande 'new exercise' pour scaffolder un exercice` + push séparé + `gh run watch`.

---

## Self-Review

**Couverture :** scaffolder + CLI + doc, 7 nouveaux tests. ✓
**Risque :** templates avec `TODO` → un exercice scaffoldé NON complété casse `validate-content` (le `solution` imprime « TODO », pas la sortie attendue) — voulu : force à compléter avant merge.
**Reporté :** édition automatique de `module.yaml` (ajout au groupe) — choix du groupe ambigu, laissé manuel.
