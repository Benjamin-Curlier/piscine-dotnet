# Plan — v5 S11 : `grade-received` persiste le résultat riche (diff post-push)

> Issue **#40** (milestone #3 « v5 », label `v5`). Branche : `v5/s11-rich-push-result`.
> Dépendances : S4 (`CheckFeedback`/`CheckOutcome`), S8 (`PushResultWatcher` + page `/resultat`).
> **Premier sprint v5.**

## Constat (code lu)
`GradeReceivedCommand.Persist` (`src/Piscine.Git`) écrit **seulement** `progress.json` (statut/attempts/
timestamp) via `ProgressStore`. Le verdict **riche** (`ResultFormatter.Format` : diff `Attendu/Obtenu`,
indice, `course_ref`) part sur **stdout** du hook → perdu. La page `/resultat` (S8) n'affiche donc qu'un
`StatusBadge` et renvoie vers `/check`. Objectif : persister le riche → `/resultat` rend un `CheckFeedback`.

## Décisions de conception
- **DTO sérialisable dans `Piscine.Core`** (sens des deps : `App`→`Git`→`Grading`→`Core` ; Core ne peut
  pas voir `GraderStatus`). Nouveau `Piscine.Core.Progression` :
  - `PushCaseResult(string GraderType, bool Passed, IReadOnlyList<string> Messages)`
  - `PushExerciseResult(string ExerciseId, string ModuleId, string Status, IReadOnlyList<PushCaseResult> Cases, string? Hint, string? CourseRef)` — `Status` en **string** (« Reussi »/« ARevoir »/« NonCorrige ») pour rester sans dépendance à Grading.
  - `PushResultDocument(IReadOnlyList<PushExerciseResult> Exercises, DateTimeOffset GradedAt)`
  - `LastPushResultStore` (load/save JSON, mêmes `JsonSerializerOptions` que `ProgressStore` ; `Load` →
    `null` si absent ; `Save` crée le dossier).
- **`PiscineLayout.LastPushResultPath => Path.Combine(StateDir, "last-push-result.json")`** (à côté de
  `progress.json`).
- **Rétro-compat** : absence de `last-push-result.json` ⇒ comportement S8 (statut-only). La page choisit
  riche si l'artefact existe **et** est postérieur/cohérent, sinon `StatusBadge`.
- **Réutiliser la résolution indice/cours** : `ResultFormatter.MatchHint` (déjà utilisé par `CheckService`)
  pour l'indice apparié ; `course_ref` du manifest. `GradeReceivedCommand` a déjà `FeedbackFor(exerciseId)`.

## Garde-fous
- **Sprint qui MODIFIE le moteur légitimement** (`Piscine.Git` = `grade-received`) — c'est l'objet de #40.
  Ne pas changer la **logique de notation** (graders) ni le **CLI** d'orchestration ; seulement **ajouter**
  la persistance d'un artefact + sa lecture côté App + le rendu page. `release.yml` **inchangé**.
- Build solution **0 warning** ; tests verts (+ nouveaux) ; `validate-content` OK ; **aucun tag**.

## Carte des fichiers
| Fichier | Action |
|---|---|
| `src/Piscine.Core/Progression/PushResultDocument.cs` (neuf) | T1 — DTO `PushResultDocument`/`PushExerciseResult`/`PushCaseResult` |
| `src/Piscine.Core/Progression/LastPushResultStore.cs` (neuf) | T1 — store JSON (load null-si-absent / save) |
| `src/Piscine.Core/PiscineLayout.cs` | T1 — `LastPushResultPath` |
| `src/Piscine.Git/GradeReceivedCommand.cs` | T2 — construire + persister le document riche dans `Persist` |
| `src/Piscine.App/Push/ProgressFileWatcher.cs` (+ `IPushResultWatcher`/`PushResult`) | T3 — exposer le résultat riche du dernier push (lire `LastPushResultStore`) |
| `src/Piscine.Components/Components/Push/PushResultPanel.razor` | T4 — rendre `CheckFeedback` (riche) si dispo, sinon `StatusBadge` |
| `tests/...` | T5 — unit (artefact écrit/format, rétro-compat) + bUnit (rendu riche) + E2E (artefact riche → diff) |

---

## T1 — Modèle + store + layout (Core)
- [ ] `PushResultDocument.cs` : les 3 records ci-dessus (camelCase JSON, enum string non requis car Status=string).
- [ ] `LastPushResultStore.cs` : calqué sur `ProgressStore` ; `PushResultDocument? Load()` (null si fichier
  absent / JSON invalide try-catch) ; `void Save(PushResultDocument)`.
- [ ] `PiscineLayout.LastPushResultPath`.
- [ ] Build Core → 0 warning. Commit : `feat(core): modele + store du dernier resultat de push riche`

## T2 — `grade-received` persiste le riche (Git)
- [ ] Dans `GradeReceivedCommand.Persist`, après `store.Save(progress)`, construire un `PushResultDocument`
  depuis `results` : pour chaque `ExerciseGradingResult` → `PushExerciseResult(ExerciseId, ModuleId?,
  Status.ToString(), Cases mappés de `result.Results` {GraderType, Passed = Status==Reussi, Messages},
  Hint = ResultFormatter.MatchHint(result, feedback), CourseRef = feedback.CourseRef)`. ModuleId : résoudre
  via `ContentLocator.FindExercise(...).ModuleId` (ou capturer le module pendant la boucle Run — préférable :
  passer le moduleId dans une structure intermédiaire). `GradedAt = DateTimeOffset.Now`.
- [ ] `new LastPushResultStore(_layout.LastPushResultPath).Save(doc)`.
- [ ] **Ne change pas** le `CommandResult`/stdout ni la logique de notation.
- [ ] Tests unit (Piscine.Git.Tests) : après `Run`, `last-push-result.json` existe, contient le bon exo,
  status, et les messages de diff pour un cas ARevoir. Commit : `feat(grade-received): persister last-push-result.json (diff riche)`

## T3 — App lit le résultat riche (Push)
- [ ] Étendre `IPushResultWatcher`/`ProgressFileWatcher` (ou ajouter une méthode `LatestRichResult()`)
  pour exposer le `PushResultDocument` courant (lecture `LastPushResultStore` à chaque settle, en plus du
  statut). Garder l'event existant ; ajouter le riche au payload **ou** une lecture à la demande par la page.
- [ ] Rétro-compat : null si artefact absent. Tests unit (Piscine.App.Tests). Commit : `feat(app): exposer le resultat de push riche (lecture last-push-result.json)`

## T4 — Page `/resultat` rend le riche (Components)
- [ ] `PushResultPanel.razor` : si un `PushResultDocument` est dispo, rendre, par exercice, un
  `CheckFeedback` (mapper `PushExerciseResult` → `CheckOutcome`/`CheckCaseResult`) ; sinon, garder le
  `StatusBadge` (S8) + lien `/check`. Auto-refresh inchangé.
- [ ] bUnit : artefact riche → diff `Attendu/Obtenu` rendu (`data-testid`). Commit : `feat(resultat): rendre le diff riche du dernier push (CheckFeedback)`

## T5 — Vérif + retex + PR (sans tag)
- [ ] `dotnet build Piscine.slnx -c Release` → 0 warning ; `dotnet test` → verts (+ nouveaux) ;
  `validate-content` OK.
- [ ] E2E (DevHost) : écrire `last-push-result.json` (via `LastPushResultStore.Save`) dans le `PISCINE_HOME`
  isolé → `/resultat` rend le diff sans clic.
- [ ] Garde : `git diff --name-only origin/main...HEAD -- src/Piscine.Grading src/Piscine.Cli .github` —
  **vide** (Grading/CLI/CI intacts ; seuls Core/Git/App/Components changent, légitime pour #40).
- [ ] Retex + PR (push/create séparés) → CI verte → squash-merge → `Fixes #40` → consigner.

Expected : `/resultat` affiche le **diff riche** du dernier push sans re-jouer le grader ; rétro-compat
statut-only conservée ; build 0 warning, tests verts, aucun tag.
