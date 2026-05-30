# Itération 13 — Support moteur des Rushes + Rush 0 — Implementation Plan

> **For agentic workers:** itération **outil + contenu** (TDD côté moteur). Choix proprio (AskUserQuestion It.12) : ajouter la découverte/grading des Rushes, puis livrer Rush 0.

**Goal:** Rendre les **Rushes** (projets de synthèse solo) first-class dans le moteur — découverts sous `content/rushes/`, notés par la moulinette (réutilise tout le pipeline d'exercices). Livrer **Rush 0 — FizzBuzz** (synthèse M01→M04).

**Architecture / approche :** Un Rush = dossier autonome `content/rushes/<id>/` portant **directement** `manifest.yaml` (réutilise `ExerciseManifest` + graders `io`/`unit`) + `subject.md` + `starter/` + `solution/`. Pas de `module.yaml`, pas de groupe. Rangé sous un pseudo-module `rushes` (workspace `ws/rushes/<id>`, snapshot `rushes/<id>`). Tri par id (`r0-`, `r1-`…). Réutilise `ContentLocator`/`SubmissionLoader`/`ExerciseGrader`/`GroupGrader`/`ProgressRecorder` sans changement de leur logique.

**Tech Stack:** C# / .NET 10, xUnit ; Markdown + YAML.

---

## Tasks

- [x] **Core (TDD)** : `Rush(Id, Title, ContentDir)` (Model) ; `ContentDiscovery.DiscoverRushes(paths)` (scan `rushes/*/manifest.yaml`, trié par id) ; `ContentLocator.FindExercise` étendu (scanne `rushes/` après les modules, renvoie `ExerciseLocation(RushesModuleId="rushes", id, dir)`). Tests : `DiscoverRushes_ReturnsRushesSortedById`, `DiscoverRushes_ReturnsEmptyWhenRushesDirectoryMissing`, `FindExercise_FindsRush_UnderRushesModuleId`.
- [x] **Grading** : `ContentValidator.Validate` valide aussi chaque Rush découvert (corrigé passe ses graders → gate CI).
- [x] **Git (TDD)** : `GradeReceivedCommand.Run` ajoute une passe Rushes (notés indépendamment, pas de stop-au-1er-KO ; snapshot `rushes/<id>`). Test `Run_GradesRush_PushedUnderRushesDir` (push `rushes/r0-demo/Demo.cs` → Réussi + progression).
- [x] **CLI** : `list` affiche une section « Rushes (projets de synthèse) ». `start`/`check` marchent via `ContentLocator` (aucun changement).
- [x] **Contenu Rush 0** : `content/rushes/r0-fizzbuzz/` (manifest `io`, subject riche avec le piège %15-avant-%3/5, starter, solution). Cas `5`, `3`, `15`, `1`.
- [x] **Vérif** : suite 77 verts (Core 32 + Grading 38 + Git 7) ; `validate-content` → « Contenu valide. » (note le Rush) ; `list` montre la section Rushes.
- [ ] **Commit** `feat(rushes): découverte + notation des Rushes, Rush 0 FizzBuzz` + push séparé + `gh run watch`.

---

## Self-Review

**Couverture (It.13) :** moteur Rushes (discovery + location + validation + grade-received + list) en réutilisant 100 % du pipeline d'exercices ; Rush 0 livré. 6 nouveaux tests (3 Core + 1 Git + couverture validate-content). ✓
**Risque :** un Rush partage le namespace d'ids des exercices pour `FindExercise`/progression (préfixe `r` évite la collision). `package-content` exclut déjà `solution/` partout → corrigés de Rush non zippés. Prouvé par `validate-content` + test git.
**Reporté :** Rush 1/2/3 à leurs checkpoints ; format du **module Git dédié** (M05) toujours à concevoir ; grader `unit` réel à M13. Note : `start` d'un Rush crée `ws/rushes/<id>` ; le rendu git mirroir = `rushes/<id>/<livrable>`.
