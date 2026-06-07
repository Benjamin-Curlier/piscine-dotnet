# Retex — v4 S10 : docs recrue/encadrant + Curriculum/CHANGELOG (flux desktop)

> Issue #31. Branche `v4/s10-docs-desktop`. Plan : [../plans/2026-06-07-v4-s10-docs-desktop.md](../plans/2026-06-07-v4-s10-docs-desktop.md).
> **Verdict : objectif atteint.** La doc recrue/encadrant décrit le **flux desktop réel** (rendu
> possible depuis S9b) sans rien sur-promettre ; CHANGELOG « Non publié » prêt à versionner.
> **Dernier sprint du backlog v4 → milestone #2 CLOS.** Sprint **docs uniquement** : aucun `src/` ni CI
> touché, aucun tag.

## Ce qui a été fait
- **`docs/mise-en-oeuvre.md`** : l'app de bureau devient l'**UX recrue principale** (§3/§4) — cours,
  *Vérifier* (`/check`), *Progression* (`/progress`), *Initialiser* (`/init`), *Résultat* (`/resultat`) ;
  le **rendu** (`git add/commit/push`) se fait au **terminal système** (Windows : `start-piscine.cmd` met
  `git` + `piscine` sur le PATH). Le **CLI** reste documenté en parallèle. Checklist encadrant : la
  fenêtre **route le flux**.
- **`CHANGELOG.md`** : section **`## [Non publié]`** — app de bureau Photino (composants, vérif,
  progression, init, résultat), packaging par OS + dry-run CI, prérequis webview ; **moteur/CLI/
  `grade-received` inchangés** ; terminal embarqué **différé**.
- **`docs/wiki/Curriculum.md`** : note « UX = app de bureau ou CLI ; contenu inchangé ».
- **`docs/deploiement.md`** : smoke pré-release « la fenêtre **route le flux** » (et plus « affiche un cours »).

## Décision d'honnêteté (clé)
La 1ʳᵉ version de la doc (S7/spike) parlait d'un **« terminal intégré »** dans l'app : **retiré**. Le
terminal embarqué + coaching git sont **différés** (dépendent du `Piscine.GitShim` hors release, cf.
retex S9b) → la doc dit explicitement que l'app n'embarque ni terminal ni éditeur, et que le `git push`
se fait au terminal système. **Documenter ce qui est livré, pas ce qui est prévu.**

## Garde-fous respectés
- `git diff --name-only origin/main...HEAD -- src .github` = **vide** (docs/CHANGELOG seulement).
- `validate-content` = « Contenu valide. » (contenu pédagogique inchangé).
- **Aucun tag.** Le CHANGELOG « Non publié » rend un futur tag trivial — mais **publier reste la décision
  du propriétaire** (cf. DÉCISIONS). Pour valider l'app sur les 3 OS : taguer une **pré-release** et
  dérouler la checklist smoke (deploiement.md §5).

## Clôture du backlog v4 (milestone #2, #22–#31)
- S1 #22 Fondation (RCL + bi-hôte) · S2 #23 PTY · S3 #24 statut git + coaching · S4 #25 check ·
  S5 #26 progression · S6 #27 cours/thème · S7 #28 init · S8 #29 surveillance push · S9 #30 packaging ·
  **S9b #42 câblage Photino** (inséré : l'app montait encore le spike S1) · **S10 #31 docs**. **Tous mergés.**
- **Reste hors backlog (suivis)** : terminal + coaching **dans Photino** (packager le shim) ; persister le
  **résultat riche** de `grade-received` (#40) ; confirmer terminal/coaching/fenêtre native Linux/macOS
  (checklists smoke proprio S2/S3/S9/S9b) ; déplacer `Error.razor` hors RCL ; attribution git par exo.

## Piège réutilisable
- **Séquencer docs APRÈS la capacité réelle** : S9 avait packagé une app encore au stade spike ; vouloir
  documenter un « flux desktop » à ce moment aurait produit une **doc mensongère**. Insérer S9b (câblage)
  avant S10 (docs) a permis une doc vraie. Quand une issue « doc » présuppose une capacité, **vérifier que
  la capacité existe** (lire le code livré) avant d'écrire — sinon créer le sprint d'implémentation manquant.
