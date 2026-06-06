# Sprint 1 (V3) — Grader `git` (issue #1)

> Scrum / loop V3. Source : roadmap v3 + BLOCKERS Point 4. Branche `feat/grader-git`.

## Objectif du sprint
Poser le **type de grading `git`** : le manifest décrit un **état attendu du dépôt** rendu par la
recrue ; le `GitGrader` inspecte le dépôt via **LibGit2Sharp** et rend un verdict éducatif.

## Découpage (re-scope scrum)
Issue #1 = ~2-3 j. Découpée pour rester *shippable* à chaque itération :
- **Sprint 1 (ce plan) — FONDATION MOTEUR** : modèle manifest `git` + `GitGrader` + suite de
  tests xUnit (dépôts temporaires LibGit2Sharp). Build + tous tests verts.
- **Sprint 1b (follow-up, nouvelle issue)** : adoption *contenu* — extraction du **vrai dépôt** côté
  `grade-received` (aujourd'hui snapshot plat sans `.git`), exos pilotes M05/M14, fixtures
  `validate-content`.

## Conception
### Modèle (`Piscine.Core/Model/GradingStep.cs`)
Nouveau bloc optionnel `git:` sur l'étape :
```yaml
grading:
  - type: git
    git:
      branches: [main, feature]     # branches qui doivent exister
      min_commits: 3                # nb min de commits atteignables depuis HEAD (0 = ignoré)
      no_conflict_markers: true     # aucun <<<<<<< ======= >>>>>>> dans l'arbre de HEAD
      files:
        - path: README.md
          ref: HEAD                 # branche/ref où lire (défaut HEAD)
          contains: "Bonjour"       # sous-chaîne requise
          content: "Bonjour\n"      # contenu exact (optionnel)
      merged:
        - into: main                # la pointe de `branch` est un ancêtre de `into`
          branch: feature
```
Classes : `GitAssertions`, `GitFileAssertion` (`Path/Ref/Contains/Content`), `GitMerge` (`Into/Branch`).

### Contexte (`Piscine.Grading/GradingContext.cs`)
Ajouter `string? RepositoryPath` (chemin du dépôt rendu à inspecter). Rétro-compatible (optionnel).

### Grader (`Piscine.Grading/GitGrader.cs`)
- `Type => "git"`. Ouvre `context.RepositoryPath` via LibGit2Sharp.
- Échecs si : repo absent/invalide ; branche manquante ; `min_commits` non atteint ; marqueurs de
  conflit présents ; fichier absent / `contains`/`content` non satisfait ; `merged` non vérifié
  (pointe de `branch` pas ancêtre de `into`).
- Collecte tous les écarts → un seul `GraderResult.Failure` éducatif, `Trigger = git_state`.
- Nouveau `FeedbackTriggers.GitState = "git_state"` (+ ajouté à `All`).

### Câblage
- `Graders.Default()` enregistre `new GitGrader()`.
- `SubmissionLoader` renseigne `RepositoryPath = workspaceExerciseDir` (le dossier rendu).

## Definition of Done (Sprint 1)
- [ ] Modèle `git` + `GitAssertions` (Core)
- [ ] `GitGrader : IGrader` + `git_state` trigger
- [ ] Enregistré dans `Graders.Default`
- [ ] Suite de tests xUnit (corrigé OK + chaque assertion KO rejetée) verte
- [ ] `dotnet test Piscine.slnx -c Release` vert (≥ 131 + nouveaux)
- [ ] Revue par agent spécialiste
- [ ] Docs MAJ (issue, HANDOFF, mémoire) + retex
- [ ] PR mergée, CI verte
