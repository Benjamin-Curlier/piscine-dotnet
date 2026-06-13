# Sprint S1 — Tableau de bord (status board) — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps en `- [ ]`.

**Goal:** Remplacer le stub `Dashboard.razor` (`/`) par le vrai **tableau de bord** : *Reprendre* (prochaine action) → *progression globale* → *résultats récents*, conformément à la spec §5.2.

**Architecture:** La logique pure va dans **`Piscine.App/Board/`** (testable sans UI, sans dépendre de `CourseCatalog` qui vit dans `Piscine.Components`) : `ResumeSelector` (choix de l'exo à reprendre) et `BoardCounts` (compteurs + %). `Dashboard.razor` (RCL) injecte `CourseCatalog` + `ProgressService` + `IPushResultWatcher`, construit la liste ordonnée d'exos + la map de statuts, appelle les helpers purs, et rend le board (réutilise `StatusBadge`/`StatusDot` + CSS existant `hero`/`module-grid`/`module-card`/barres). **Invariant : moteur / `Piscine.Cli` / `grade-received` / `release.yml` intacts.**

**Décisions :**
- *Reprendre* = 1er exo **actionnable** dans l'ordre du curriculum : priorité `ARevoir` > `EnCours`/`CommiteNonPousse` > `NonCommence` ; `null` si tout est `PousseNote` (→ message « tout est à jour »).
- *Progression globale* = compteurs (Fait=`PousseNote` / En cours=`EnCours`+`CommiteNonPousse` / À revoir=`ARevoir` / Restant=`NonCommence`) + % (Fait/total) + **mini-barre par module** (PousseNote/total, via comptage inline).
- *Résultats récents* = `IPushResultWatcher.LatestResult()` (dernier delta de push de la session) ; **état vide** sympa si `null` (« Pousse ton travail pour voir tes derniers résultats ici »). Pas de nouvelle persistance (historique daté = amélioration ultérieure).

**Conventions :** commits FR + trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` ; commit ≠ push (appels séparés, pas de push dans ce plan) ; build `TreatWarningsAsErrors` → 0 warning ; bUnit `BunitContext`/`Render<T>()` ; E2E skip-sans-Chromium.

---

## Tâche 1 : `ResumeSelector` (pur) — Piscine.App

**Files:** Create `src/Piscine.App/Board/ResumeSelector.cs` ; Test `tests/Piscine.App.Tests/ResumeSelectorTests.cs`.

Test d'abord (rouge → vert) :
```csharp
using Piscine.App.Board;
using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class ResumeSelectorTests
{
    private static (string, string) Exo(string m, string e) => (m, e);

    [Fact]
    public void Empty_curriculum_returns_null() =>
        Assert.Null(ResumeSelector.Pick([], new Dictionary<(string, string), ExerciseProgressStatus>()));

    [Fact]
    public void All_done_returns_null()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
            [("m", "b")] = ExerciseProgressStatus.PousseNote,
        };
        Assert.Null(ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void Picks_first_NonCommence_when_nothing_in_progress()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
        };
        Assert.Equal(("m", "b"), ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void In_progress_beats_later_NonCommence()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b"), Exo("m", "c") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.PousseNote,
            [("m", "b")] = ExerciseProgressStatus.EnCours,
        };
        Assert.Equal(("m", "b"), ResumeSelector.Pick(exos, st));
    }

    [Fact]
    public void ARevoir_has_highest_priority_even_if_later()
    {
        var exos = new[] { Exo("m", "a"), Exo("m", "b"), Exo("m", "c") };
        var st = new Dictionary<(string, string), ExerciseProgressStatus>
        {
            [("m", "a")] = ExerciseProgressStatus.EnCours,
            [("m", "c")] = ExerciseProgressStatus.ARevoir,
        };
        Assert.Equal(("m", "c"), ResumeSelector.Pick(exos, st));
    }
}
```
Impl :
```csharp
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Progress;

namespace Piscine.App.Board;

/// <summary>
/// Choisit l'exercice à « Reprendre » : 1er exo actionnable dans l'ordre du curriculum. Priorité
/// décroissante : ARevoir, puis travail en cours (EnCours/CommiteNonPousse), puis NonCommence.
/// Renvoie null si tous les exos sont PousseNote (rien à reprendre). Pur.
/// </summary>
public static class ResumeSelector
{
    public static (string ModuleId, string ExerciseId)? Pick(
        IReadOnlyList<(string ModuleId, string ExerciseId)> orderedExercises,
        IReadOnlyDictionary<(string ModuleId, string ExerciseId), ExerciseProgressStatus> statuses)
    {
        static ExerciseProgressStatus Status(
            (string, string) key,
            IReadOnlyDictionary<(string, string), ExerciseProgressStatus> s)
            => s.TryGetValue(key, out var v) ? v : ExerciseProgressStatus.NonCommence;

        (string, string)? First(System.Func<ExerciseProgressStatus, bool> match)
            => orderedExercises.Cast<(string, string)?>()
                .FirstOrDefault(e => match(Status(e!.Value, statuses)));

        return First(s => s == ExerciseProgressStatus.ARevoir)
            ?? First(s => s is ExerciseProgressStatus.EnCours or ExerciseProgressStatus.CommiteNonPousse)
            ?? First(s => s == ExerciseProgressStatus.NonCommence);
    }
}
```
Run: `dotnet test tests/Piscine.App.Tests -c Release --filter "FullyQualifiedName~ResumeSelectorTests"` (5 verts). Commit `feat(qol/s1): ResumeSelector (choix de l'exo à reprendre)`.

---

## Tâche 2 : `BoardCounts` (pur) — Piscine.App

**Files:** Create `src/Piscine.App/Board/BoardCounts.cs` ; Test `tests/Piscine.App.Tests/BoardCountsTests.cs`.

Test :
```csharp
using Piscine.App.Board;
using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class BoardCountsTests
{
    [Fact]
    public void Counts_and_percent_are_correct()
    {
        ExerciseProgressStatus[] s =
        [
            ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.PousseNote,
            ExerciseProgressStatus.EnCours, ExerciseProgressStatus.CommiteNonPousse,
            ExerciseProgressStatus.ARevoir, ExerciseProgressStatus.NonCommence,
        ];
        var c = BoardCounts.From(s);
        Assert.Equal(2, c.Fait);
        Assert.Equal(2, c.EnCours);
        Assert.Equal(1, c.ARevoir);
        Assert.Equal(1, c.Restant);
        Assert.Equal(6, c.Total);
        Assert.Equal(33, c.PercentFait); // 2/6 = 33 % (arrondi)
    }

    [Fact]
    public void Empty_is_all_zero_no_divide_by_zero()
    {
        var c = BoardCounts.From([]);
        Assert.Equal(0, c.Total);
        Assert.Equal(0, c.PercentFait);
    }
}
```
Impl :
```csharp
using System.Collections.Generic;
using System.Linq;
using Piscine.App.Progress;

namespace Piscine.App.Board;

/// <summary>Compteurs agrégés du tableau de bord (pur). Fait = PousseNote ; En cours = EnCours +
/// CommiteNonPousse ; À revoir = ARevoir ; Restant = NonCommence. % = Fait / Total (arrondi).</summary>
public sealed record BoardCounts(int Fait, int EnCours, int ARevoir, int Restant, int Total)
{
    public int PercentFait => Total == 0 ? 0 : (int)System.Math.Round(100.0 * Fait / Total);

    public static BoardCounts From(IReadOnlyList<ExerciseProgressStatus> statuses)
    {
        int Count(ExerciseProgressStatus s) => statuses.Count(x => x == s);
        var enCours = Count(ExerciseProgressStatus.EnCours) + Count(ExerciseProgressStatus.CommiteNonPousse);
        return new BoardCounts(
            Fait: Count(ExerciseProgressStatus.PousseNote),
            EnCours: enCours,
            ARevoir: Count(ExerciseProgressStatus.ARevoir),
            Restant: Count(ExerciseProgressStatus.NonCommence),
            Total: statuses.Count);
    }
}
```
Run filter `BoardCountsTests` (2 verts). Commit `feat(qol/s1): BoardCounts (compteurs + % du board)`.

---

## Tâche 3 : `Dashboard.razor` — le board — Piscine.Components

**Files:** Modify `src/Piscine.Components/Components/Pages/Dashboard.razor` (+ `Dashboard.razor.css` si besoin) ; Test `tests/Piscine.Components.Tests/DashboardTests.cs`.

Le composant (`@page "/"`, `@rendermode InteractiveServer`, namespace `Piscine.Components.Components.Pages`) :
- `@inject CourseCatalog Catalog`, `@inject ProgressService Progress`, `@inject IPushResultWatcher Pushes`.
- `OnInitialized` : `_exos` = liste ordonnée `(ModuleId, Id)` de tous les exos ; `_status` = map via `Progress.SnapshotFor(_exos)` ; `_resume = ResumeSelector.Pick(_exos, _status)` ; `_counts = BoardCounts.From(_status.Values)` ; `_recent = Pushes.LatestResult()`.
- Rendu (data-testid pour E2E/bUnit) :
  - `data-testid="dashboard"` (landmark, conservé) ;
  - **Reprendre** : si `_resume != null`, carte `data-testid="board-resume"` avec lien `href="/module/{m}/{e}"`, titre de l'exo (via `Catalog.GetExercise`), et `<StatusBadge Status=...>` ; sinon message « Tout est à jour ✅ ».
  - **Progression** : 4 cartes compteurs (`data-testid="board-count-fait/encours/arevoir/restant"`) + `_counts.PercentFait` (`data-testid="board-percent"`) ; **mini-barres par module** : pour chaque `m` avec exos, `done = _status` count PousseNote / `m.ExerciseCount`, barre + `done/total`.
  - **Résultats récents** : si `_recent != null && _recent.Changed.Count>0`, lister chaque `PushResultEntry` (exo + `<StatusBadge>` mappé : Reussi→PousseNote, ARevoir→ARevoir) + lien `/check` ; sinon état vide `data-testid="board-recent-empty"`.

bUnit (`DashboardTests`) — enregistrer en DI : `CourseCatalog` (via un `IConfiguration` pointant `PISCINE_CONTENT` de test, ou un catalog réel sur un content fixture), `ProgressService` (layout temp), un `IPushResultWatcher` factice. **Plus simple** : tester surtout le rendu conditionnel via un faux `IPushResultWatcher` (retourne un `PushResult` planté) et un `ProgressService` sur workspace temp vide (tout NonCommence → resume = 1er exo). Assertions : présence `board-resume` (href vers 1er exo), `board-percent`=0, et `board-recent-empty` quand le watcher renvoie null ; avec un watcher renvoyant 1 entrée Reussi → une ligne de résultat. (Si l'injection de `CourseCatalog` est lourde en bUnit, couvrir le rendu complet par l'E2E Tâche 4 et limiter le bUnit aux branches sans catalog — découper un sous-composant `BoardOverview`/`BoardRecent` prenant les données en paramètres, testable sans DI. **Recommandé : extraire `BoardOverview.razor`** (paramètres `Counts`, `Modules done/total`) et `BoardRecent.razor` (paramètre `PushResult?`) → bUnit pur par paramètres ; `Dashboard.razor` orchestre.)

Run : build RCL 0 warning + `dotnet test tests/Piscine.Components.Tests -c Release`. Commit `feat(qol/s1): tableau de bord (reprendre + progression + résultats récents)`.

---

## Tâche 4 : E2E board — DevHost

**Files:** Create `tests/Piscine.DevHost.E2E/BoardSmokeTests.cs` (port dédié **5259**).

Modelé sur `NavigationSmokeTests` : démarre le DevHost avec un `progress.json` planté (ex00-hello `ARevoir`), va sur `/`, attend `[data-testid="dashboard"]`, asserte : une carte `[data-testid="board-resume"]` présente (lien `/module/...`), `[data-testid="board-percent"]` présent, au moins une mini-barre de module. Skip propre sans Chromium. Run filtré. Commit `test(qol/s1): E2E tableau de bord`.

---

## Tâche 5 : Vérification finale

- [ ] `dotnet test Piscine.slnx -c Release` → tout vert, **0 warning**.
- [ ] Smoke de rendu **local** (gate desktop) : `PISCINE_SMOKE=1 PISCINE_SMOKE_OUT=/tmp/s.json PISCINE_CONTENT="$PWD/content" dotnet run --project src/Piscine.Desktop -c Release` → `received:true`, `dashboard:true`, et `appTextLen` accru (board rempli). (Rappel : pas de CI pour ce smoke.)
- [ ] `validate-content` OK.

---

## Notes de revue
- Couverture spec §5.2 : reprendre (T1), progression+%+barres (T2/T3), résultats récents (T3). ✔
- Invariants : seuls `Piscine.App/Board/`, `Piscine.Components` (Dashboard + sous-composants), tests changent. Moteur/CLI/`grade-received`/`release.yml` intacts. ✔
- Hors S1 (plus tard) : historique daté des résultats (LastAttempt), filtres, toast de push global (S4).
