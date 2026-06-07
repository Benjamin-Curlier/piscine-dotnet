# Plan — #17 Notation live des exercices git côté `grade-received`

> Suite de #9 (grader git utilisable en contenu + validé par fixture). Reste : **noter les exos `git`
> au push**, contre le **dépôt bare** (`RemoteRepoPath`), sans « à revoir » parasites pour les exos non
> commencés. Branche `feat/17-notation-live-git`.

## Constats (exploration)
- `GradeReceivedCommand.Run(sha)` extrait l'**arbre plat** du commit (`CommitExtractor`, **sans `.git`**)
  puis note les exos io/unit/projet par sous-dossier. Un exo **`git`** (`deliverables: []`) n'a **pas** de
  sous-dossier rendu → il est **silencieusement sauté** aujourd'hui.
- `GitGrader` note contre `context.RepositoryPath` ; il lit `repo.Head?.Tip` pour `min_commits` et
  `no_conflict_markers`. **Problème bare** : après `git push origin main`, le dépôt bare a la ref
  `refs/heads/main` mais son **HEAD reste sur `master` (orphelin)** → `repo.Head.Tip == null`. Et
  `git push origin main` **ne pousse que `main`** (la branche `feature` n'arrive pas au bare).
- Le hook `post-receive` appelle `grade-received <newrev>` **une fois par ref** poussée.
- La gate `validate-content` construit une **fixture** (dépôt non-bare, HEAD sur `main` après merge) et la
  confronte aux assertions — **doit rester verte** (mes changements sont rétro-compatibles : HeadRef null
  ⇒ `repo.Head`).

## Décisions de conception
1. **Grader contre le dépôt bare, branche de rendu = `main` comme « HEAD ».** Indépendant de la ref qui a
   déclenché le hook ⇒ verdict **idempotent** même si `git push --all` déclenche plusieurs fois. Nouveau
   champ `GradingContext.HeadRef` (null = `repo.Head`, inchangé pour check local + fixture).
2. **Signal « tenté » explicite et déclaratif** : bloc `attempt:` dans le manifest `git`. **Sans `attempt`,
   un exo git n'est PAS noté en live** (conservateur : zéro « à revoir » parasite ; la fixture le valide
   toujours). Avec `attempt`, noté **dès** que le signal est présent dans le bare (ex. branche `feature`).
   - `attempt: { branch: feature }` et/ou `{ file: { path, ref } }`. Tenté = au moins un prédicat vrai.
3. **L'exo M05 doit pousser ses branches** : sujet mis à jour (`git push origin --all`) pour que `feature`
   atteigne le bare (sinon `branches: [feature]` et le signal `attempt` ne peuvent pas être satisfaits).

## Lots (TDD)
| # | Changement | Tests |
|---|---|---|
| L1 | `GradingContext.HeadRef` (Core/Grading) ; `GitGrader` résout le head via HeadRef | GitGrader bare-style : `min_commits`/`no_conflict_markers` OK contre un bare avec HeadRef=main ; null ⇒ inchangé |
| L2 | Modèle `GitAttempt` + `GitAssertions.Attempt` (parse YAML underscore) | désérialisation `attempt:` |
| L3 | `GitAttemptEvaluator.IsAttempted(attempt, repoPath)` | null⇒false ; branche présente⇒true ; absente⇒false ; fichier présent⇒true |
| L4 | `GradeReceivedCommand` : passe git (exos `attempt` satisfaits notés contre le bare, HeadRef=main, dans la sémantique de groupe) | intégration : feature+main poussés⇒Réussi ; main seul (feature absent)⇒**sauté** (pas d'à-revoir) ; feature non mergée⇒à-revoir |
| L5 | `ContentValidator` : si `attempt` déclaré, la **fixture** doit le satisfaire | la fixture M05 (feature existe) passe ; un `attempt` non satisfait par la fixture ⇒ issue |
| L6 | M05 manifest `attempt: { branch: feature }` + subject `git push origin --all` | `validate-content` vert |

## Garde
- `git diff origin/main -- src/Piscine.Cli` = vide (CLI inchangé). Moteur io/unit/projet/mutation/reseau
  intact. Build 0 warning. Tous les tests verts. `validate-content` vert. **Aucun tag.**
