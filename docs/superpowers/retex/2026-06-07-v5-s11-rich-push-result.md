# Retex — v5 S11 : `grade-received` persiste le résultat riche (diff post-push)

> Issue #40. Branche `v5/s11-rich-push-result`. Plan : [../plans/2026-06-07-v5-s11-rich-push-result.md](../plans/2026-06-07-v5-s11-rich-push-result.md).
> **Verdict : objectif atteint.** `/resultat` affiche désormais le **diff riche** (attendu/obtenu +
> indice + lien cours) du dernier push, **sans re-jouer le grader**. Rétro-compat statut-only conservée.
> **Premier sprint v5.** Build 0 warning ; **253 tests verts** ; Grading/CLI/CI intacts ; aucun tag.

## Ce qui a été fait (par couche)
- **Core** : `PushResultDocument`/`PushExerciseResult`/`PushCaseResult` (DTO **sans dépendance au moteur**
  — `Status` en chaîne, car `Piscine.Core` ne voit pas `GraderStatus`) + `LastPushResultStore`
  (JSON camelCase, `Load`→`null` si absent/illisible) + `PiscineLayout.LastPushResultPath`.
- **Git (`grade-received`)** : `Persist` écrit **en plus** de `progress.json` un `last-push-result.json`
  riche (par exo : statut, cas/diff verbatim, indice apparié, `course_ref`). **Best-effort** (IOException
  ignorée). Résolution indice/cours **identique** à `ResultFormatter`/`CheckService`. La logique de
  notation et le `CommandResult`/stdout sont **inchangés**.
- **App** : `IPushResultWatcher.LatestRichResult()` lit le document **à la demande** (null si absent).
- **Components** : `PushResultPanel` mappe `PushExerciseResult` → `CheckOutcome` et rend **`CheckFeedback`**
  (S4) inline par exo quand l'artefact riche existe ; sinon `StatusBadge` + lien `/check` (S8).

## Décisions
- **DTO dans Core, pas réutilisation de `CheckOutcome`** : le sens des dépendances est `App`→`Git`→`Core` ;
  `Git` ne peut pas voir `Piscine.App.Checking.CheckOutcome`. Le DTO Core est neutre ; le mapping vers
  `CheckOutcome` se fait côté UI (Components) pour réutiliser `CheckFeedback` sans dupliquer le rendu.
- **Lecture à la demande** (pas un 2ᵉ FileSystemWatcher sur `last-push-result.json`) : `grade-received`
  écrit `progress.json` **puis** `last-push-result.json` dans le **même** `Persist` synchrone → au settle
  du watcher existant (sur `progress.json`), l'artefact riche est déjà là. Plus simple, pas de 2ᵉ watcher.
- **Rétro-compat verrouillée** : artefact absent ⇒ `null` ⇒ comportement S8 (statut + lien `/check`).
  Testé aux 3 niveaux (App null, page fallback, Git n'écrit que si gradé).

## Prouvé (automatique)
- **Git** (9, +2) : après `Run`, `last-push-result.json` contient l'exo, le `ModuleId`, le `Status`, le
  `course_ref`, et le **diff** (`Attendu`/`Obtenu`) pour un cas ARevoir ; `Status` correct pour Réussi.
- **App** (53, +2) : `LatestRichResult()` = document quand présent, `null` quand absent.
- **Components** bUnit (25, +1) : artefact riche → `diff-expected`/`diff-actual`/`check-verdict`/
  `check-hint`/`check-course-ref` rendus inline, **pas** de `push-check-link`.
- **E2E** (9, +1) : DevHost réel, `/resultat` lit l'artefact sur disque et rend le diff **sans clic**.
- `validate-content` OK ; garde `git diff origin/main...HEAD -- src/Piscine.Grading src/Piscine.Cli .github`
  = **vide**.

## Limites / suites
- Le diff reste du **texte verbatim** (`Messages`), comme `/check` (S4) — non structuré ; rendu via `@expr`
  (HTML-encodé, pas de `MarkupString`). Cohérent avec l'existant.
- `last-push-result.json` reflète **le dernier push** (écrasé à chaque rendu) — voulu pour `/resultat`.

## Piège réutilisable
- **Artefact partagé entre moteur (écrivain) et UI (lecteur)** = DTO dans la **couche commune basse**
  (`Core`), `Status`/enums en **chaîne** pour ne pas tirer la couche moteur dans Core ; le mapping vers
  les modèles UI riches se fait côté UI. Évite une dépendance inversée `Core`→`Grading`/`App`.
