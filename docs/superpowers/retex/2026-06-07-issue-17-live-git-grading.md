# Retex — #17 : notation live des exercices git côté `grade-received`

> Issue #17 (enhancement V3, suite de #9). Branche `feat/17-notation-live-git`.
> Plan : [../plans/2026-06-07-issue-17-live-git-grading.md](../plans/2026-06-07-issue-17-live-git-grading.md).
> **Verdict : objectif atteint.** Les exos `git` sont notés **au push**, contre le **dépôt bare**, sans
> « à revoir » parasite pour les exos non commencés. **267 tests** (+10) ; CLI + autres graders intacts ;
> `validate-content` vert ; **aucun tag.**

## Ce qui a été fait
- **`GradingContext.HeadRef`** (nouveau, optionnel) : branche à traiter comme `HEAD`. `null` ⇒ `repo.Head`
  (check local + fixture inchangés). Côté `grade-received`, on passe `"main"` (la branche de rendu).
- **`GitGrader`** : résout le HEAD effectif via `HeadRef` (`ResolveHead`) ; `min_commits` et
  `no_conflict_markers` walk depuis ce commit (`CommitFilter.IncludeReachableFrom`). Rétro-compatible.
- **`GitAttempt`** (`branch` et/ou `file`) sur `GitAssertions.Attempt` + **`GitAttemptEvaluator`** : l'exo
  n'est noté en live **que** si un signal « tenté » est déclaré **et** satisfait dans le bare. `null` ⇒
  pas de notation live (zéro « à revoir » parasite).
- **`GradeReceivedCommand`** : passe git intégrée à la boucle de groupe — un exo git tenté est noté contre
  le bare (`HeadRef="main"`, dans la sémantique stop-au-1er-KO) ; non tenté ⇒ sauté comme un exo non rendu.
- **`ContentValidator`** : si `attempt` est déclaré, la **fixture** (corrigé) doit le satisfaire (sinon le
  vrai rendu ne serait jamais reconnu « tenté »).
- **M05** : manifest `attempt: { branch: feature }` ; subject mis à jour → **`git push origin --all`**
  (pour que `feature` atteigne le bare ; `git push origin main` seul ne pousse pas `feature`).

## Constats clés (réutilisables)
- **Dépôt bare après push : HEAD orphelin.** `Repository.Init(bare)` pointe HEAD sur `master` (orphelin) ;
  un `push origin main` crée `refs/heads/main` **sans** déplacer HEAD ⇒ `repo.Head.Tip == null`. Tout
  grader qui lit `repo.Head` sur un bare doit recevoir la branche cible explicitement (`HeadRef`).
- **`git push origin main` ne pousse que `main`.** Pour un exo qui note des **branches** (`feature`), le
  sujet doit demander `git push origin --all`, sinon ni l'assertion `branches` ni le signal `attempt` ne
  peuvent être satisfaits.
- **Signal « tenté » explicite > heuristique.** Un bloc `attempt` déclaratif (branche/fichier) évite les
  faux « non commencé » et reste honnête/testable. Défaut conservateur : **pas d'`attempt` ⇒ pas de
  notation live** (la fixture valide quand même le corrigé).
- **Idempotence du double-firing.** Le hook `post-receive` appelle `grade-received` **une fois par ref** ;
  en notant toujours `main` (et non la ref déclencheuse), le verdict git est stable même avec `push --all`.

## Tests (TDD)
- `GitLiveGradingTests` (Grading, +7) : grade bare avec HeadRef=main ⇒ Réussi ; sans HeadRef ⇒ échec
  `min_commits` (prouve le besoin + non-régression du défaut) ; `IsAttempted` null/branche présente/absente/
  fichier présent/dépôt invalide.
- `GradeReceivedCommandTests` (Git, +3) : feature+main poussés & mergé ⇒ Réussi + progression ; main seul
  ⇒ **sauté** (pas d'« à revoir », exo absent de la progression) ; feature poussée non mergée ⇒ à revoir.

## Limites / suites
- Le `git push origin --all` requis pour M05 est une **contrainte de contenu** (documentée dans le sujet),
  pas une limite moteur. Un futur exo git suit le même modèle (`attempt` + push des branches).
- Attribution par exo encore globale (le bare est partagé) : un seul exo git pilote aujourd'hui (M05) ⇒
  pas d'ambiguïté. À revisiter si plusieurs exos git coexistent dans des modules distincts.
