# Sprint S0 — Audit UX + fondation de navigation — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Poser la fondation de navigation de l'épic QoL — destinations en données, fusion de la nav redondante, routage `/`→tableau de bord et `/cours`→catalogue, pastilles de statut dans la sidebar — et produire l'audit UX qui guidera les sprints suivants.

**Architecture:** Toute l'UI vit dans la RCL `Piscine.Components` (testée via `Piscine.DevHost` + bUnit + Playwright) ; la logique pure (rollup de statut module) va dans `Piscine.App`. Les *destinations* de navigation deviennent une **liste de données** rendue par la coquille — `MainLayout` la rend en onglets aujourd'hui (Approche A), un futur rail d'icônes (B) rendra la même liste sans toucher aux pages. **Moteur, `Piscine.Cli`, `grade-received` et `release.yml` ne sont pas touchés.**

**Tech Stack:** .NET 10, Blazor (RCL `Microsoft.NET.Sdk.Razor`), Photino.Blazor (hôte), bUnit 2.x (`BunitContext` / `Render<T>()`), xUnit, Playwright 1.60 (E2E DevHost, skip sans navigateur), `dotnet build/test Piscine.slnx -c Release` (`TreatWarningsAsErrors`).

**Conventions du dépôt (rappel) :** commits **conventionnels en français**, terminés par
`Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`. **`commit` et `push` sont des
appels séparés** ; ce plan ne pousse jamais (la PR/merge est une action du proprio). En dev, exporter
`PISCINE_CONTENT` vers `content/` pour `dotnet run`. CRLF Windows bénins (`.editorconfig` = LF).

---

## Structure des fichiers (S0)

**Créés**
- `src/Piscine.Components/Navigation/NavDestination.cs` — record d'une destination primaire.
- `src/Piscine.Components/Navigation/NavDestinations.cs` — liste `Primary` + `IsActive` (pur).
- `src/Piscine.Components/Components/Layout/NavTabs.razor` (+ `.razor.css`) — barre d'onglets en haut.
- `src/Piscine.Components/Components/Progress/ExerciseProgressStatusText.cs` — libellé FR + suffixe CSS partagés (DRY).
- `src/Piscine.Components/Components/Progress/StatusDot.razor` (+ `.razor.css`) — pastille compacte.
- `src/Piscine.Components/Components/Pages/Dashboard.razor` — `@page "/"` (stub, étoffé en S1).
- `src/Piscine.Components/Components/Pages/Cours.razor` — `@page "/cours"` (grille de modules, ex-`Home`).
- `src/Piscine.App/Progress/ProgressRollup.cs` — statut représentatif d'un module (pur).
- `docs/superpowers/audits/2026-06-13-ux-audit.md` — livrable d'audit UX (Tâche 1).
- Tests : `NavDestinationsTests.cs`, `NavTabsTests.cs`, `ExerciseProgressStatusTextTests.cs`,
  `StatusDotTests.cs` (dans `tests/Piscine.Components.Tests/`) ; `ProgressRollupTests.cs`
  (dans `tests/Piscine.App.Tests/`) ; `NavigationSmokeTests.cs` (dans `tests/Piscine.DevHost.E2E/`).

**Modifiés**
- `src/Piscine.Components/Components/Layout/MainLayout.razor` — `<NavTabs />` remplace le lien « Curriculum ».
- `src/Piscine.Components/Components/Layout/NavMenu.razor` — retrait des 6 liens d'action plats ;
  ajout des pastilles (par exo + rollup module) ; injection de `ProgressService`.
- `src/Piscine.Components/Components/Progress/StatusBadge.razor` — réutilise `ExerciseProgressStatusText` (DRY).
- `tests/Piscine.DevHost.E2E/SmokeTests.cs` — pointe `/cours` (le catalogue n'est plus sur `/`).

**Supprimé**
- `src/Piscine.Components/Components/Pages/Home.razor` — remplacé par `Dashboard.razor` (`/`) + `Cours.razor` (`/cours`).

---

## Tâche 1 : Audit UX en conditions réelles (livrable doc)

**Files:**
- Create: `docs/superpowers/audits/2026-06-13-ux-audit.md`

- [ ] **Step 1 : Lancer le DevHost avec le contenu du dépôt**

Run (PowerShell, à la racine du dépôt) :
```
$env:PISCINE_CONTENT = "$PWD/content"; dotnet run --project src/Piscine.DevHost --urls http://localhost:5244
```
Expected : le serveur démarre ; `http://localhost:5244` répond. (Laisser tourner dans un terminal dédié.)

- [ ] **Step 2 : Visiter chaque route et consigner les constats**

Visiter, dans un navigateur : `/` (curriculum actuel), `/module/01-...`, une page d'exercice
`/module/01-.../ex00-...`, `/progress`, `/check`, `/init`, `/resultat`, `/terminal`. Pour chacune,
noter : lisibilité (contraste, taille, hiérarchie), ergonomie (actions manquantes, langage « console »),
incohérences de navigation. Capturer une image si possible (facultatif).

- [ ] **Step 3 : Écrire l'audit**

Créer `docs/superpowers/audits/2026-06-13-ux-audit.md` avec un tableau :

```markdown
# Audit UX — app de bureau (avant épic QoL) — 2026-06-13

> Conditions : DevHost (rend la même RCL que Photino), `PISCINE_CONTENT` = content/ du dépôt.
> Constats destinés à guider S0 (nav) et S7 (passe lisibilité/a11y).

| Page | Constat | Type (lisibilité / ergonomie / nav) | Sévérité | Sprint cible |
|------|---------|--------------------------------------|----------|--------------|
| `/` (accueil) | Le hero prescrit `piscine start` (langage console) | ergonomie | moyenne | S2 |
| sidebar | Aucun statut sur l'arbre des modules | nav | moyenne | S0 |
| barre du haut | « Curriculum » + « Accueil » redondants vers `/` | nav | faible | S0 |
| page d'exercice | Lecture seule, aucune action (vérifier/ouvrir) | ergonomie | haute | S2 |
| `/check` | Diff rendu en texte verbatim | lisibilité | moyenne | S4 |
| … | (compléter à partir des observations réelles) | | | |
```

Compléter avec les **observations réelles** relevées au Step 2 (au moins une ligne par page visitée).

- [ ] **Step 4 : Arrêter le DevHost**

Dans le terminal dédié : `Ctrl+C`.

- [ ] **Step 5 : Commit**

```
git add docs/superpowers/audits/2026-06-13-ux-audit.md
git commit -m "docs(qol/s0): audit UX de l'app de bureau avant l'épic"
```

---

## Tâche 2 : Destinations de navigation en données (`NavDestinations`)

**Files:**
- Create: `src/Piscine.Components/Navigation/NavDestination.cs`
- Create: `src/Piscine.Components/Navigation/NavDestinations.cs`
- Test: `tests/Piscine.Components.Tests/NavDestinationsTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Components.Tests/NavDestinationsTests.cs` :
```csharp
using System.Linq;
using Piscine.Components.Navigation;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class NavDestinationsTests
{
    [Fact]
    public void Primary_lists_destinations_in_expected_order()
    {
        var routes = NavDestinations.Primary.Select(d => d.Route).ToArray();
        Assert.Equal(
            new[] { "/", "/cours", "/progress", "/check", "/init", "/resultat", "/terminal" },
            routes);
    }

    [Fact]
    public void Every_destination_has_label_and_testid()
    {
        Assert.All(NavDestinations.Primary, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Label));
            Assert.False(string.IsNullOrWhiteSpace(d.TestId));
        });
    }

    [Fact]
    public void IsActive_root_matches_only_empty_path()
    {
        var dashboard = NavDestinations.Primary.First(d => d.Route == "/");
        Assert.True(NavDestinations.IsActive(dashboard, ""));
        Assert.False(NavDestinations.IsActive(dashboard, "cours"));
    }

    [Fact]
    public void IsActive_matches_first_segment_only()
    {
        var cours = NavDestinations.Primary.First(d => d.Route == "/cours");
        Assert.True(NavDestinations.IsActive(cours, "cours"));
        var terminal = NavDestinations.Primary.First(d => d.Route == "/terminal");
        Assert.True(NavDestinations.IsActive(terminal, "terminal?cwd=foo"));
    }

    [Fact]
    public void IsActive_exercise_page_activates_no_primary_tab()
    {
        Assert.All(NavDestinations.Primary, d =>
            Assert.False(NavDestinations.IsActive(d, "module/05-git/ex00-branche-merge")));
    }
}
```

- [ ] **Step 2 : Lancer le test pour le voir échouer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~NavDestinationsTests"`
Expected : ÉCHEC de compilation (`NavDestinations` / `NavDestination` n'existent pas).

- [ ] **Step 3 : Implémenter le record**

`src/Piscine.Components/Navigation/NavDestination.cs` :
```csharp
namespace Piscine.Components.Navigation;

/// <summary>
/// Une destination primaire de navigation, en données : la coquille la rend en onglet (Approche A)
/// et, plus tard, en rail d'icônes (B) sans changer les pages.
/// </summary>
public sealed record NavDestination(string Label, string Route, string TestId);
```

- [ ] **Step 4 : Implémenter la liste + `IsActive`**

`src/Piscine.Components/Navigation/NavDestinations.cs` :
```csharp
using System;
using System.Collections.Generic;

namespace Piscine.Components.Navigation;

/// <summary>
/// Catalogue des destinations primaires (en données) + règle d'activation par premier segment d'URL.
/// Pur et sans état → testable sans rendu. Rapport (/rapport) et Réglages (/reglages) s'ajouteront
/// à leurs sprints respectifs (S5/S6) ; Vérifier/Initialiser/Résultat seront absorbés par S2/S4/S7.
/// </summary>
public static class NavDestinations
{
    public static IReadOnlyList<NavDestination> Primary { get; } =
    [
        new NavDestination("Tableau de bord", "/", "nav-dashboard"),
        new NavDestination("Cours", "/cours", "nav-cours"),
        new NavDestination("Progression", "/progress", "nav-progress"),
        new NavDestination("Vérifier", "/check", "nav-check"),
        new NavDestination("Initialiser", "/init", "nav-init"),
        new NavDestination("Résultat", "/resultat", "nav-resultat"),
        new NavDestination("Terminal", "/terminal", "nav-terminal"),
    ];

    /// <summary>
    /// Vrai si <paramref name="currentRelativePath"/> (chemin relatif à la base, sans slash de tête —
    /// tel que renvoyé par <c>NavigationManager.ToBaseRelativePath</c>) appartient à la destination.
    /// La racine "/" n'est active que pour le chemin vide ; sinon comparaison du premier segment.
    /// </summary>
    public static bool IsActive(NavDestination destination, string currentRelativePath)
    {
        var path = currentRelativePath.Split('?', '#')[0].Trim('/');
        var route = destination.Route.Trim('/');

        if (route.Length == 0)
        {
            return path.Length == 0;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var first = segments.Length > 0 ? segments[0] : string.Empty;
        return string.Equals(first, route, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 5 : Lancer le test pour le voir passer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~NavDestinationsTests"`
Expected : PASS (5 tests).

- [ ] **Step 6 : Commit**

```
git add src/Piscine.Components/Navigation/ tests/Piscine.Components.Tests/NavDestinationsTests.cs
git commit -m "feat(qol/s0): destinations de navigation en données + IsActive"
```

---

## Tâche 3 : Composant `NavTabs` (barre d'onglets en haut)

**Files:**
- Create: `src/Piscine.Components/Components/Layout/NavTabs.razor`
- Create: `src/Piscine.Components/Components/Layout/NavTabs.razor.css`
- Test: `tests/Piscine.Components.Tests/NavTabsTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Components.Tests/NavTabsTests.cs` :
```csharp
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Piscine.Components.Components.Layout;
using Piscine.Components.Navigation;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class NavTabsTests : BunitContext
{
    [Fact]
    public void Renders_one_link_per_primary_destination()
    {
        var cut = Render<NavTabs>();

        foreach (var d in NavDestinations.Primary)
        {
            var link = cut.Find($"[data-testid='{d.TestId}']");
            Assert.Equal(d.Route, link.GetAttribute("href"));
            Assert.Contains(d.Label, link.TextContent, System.StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Marks_active_destination_from_current_uri()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/cours");

        var cut = Render<NavTabs>();

        Assert.Contains("active", cut.Find("[data-testid='nav-cours']").GetAttribute("class"));
        Assert.DoesNotContain("active", cut.Find("[data-testid='nav-dashboard']").GetAttribute("class") ?? "");
    }
}
```

- [ ] **Step 2 : Lancer le test pour le voir échouer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~NavTabsTests"`
Expected : ÉCHEC de compilation (`NavTabs` n'existe pas).

- [ ] **Step 3 : Implémenter le composant**

`src/Piscine.Components/Components/Layout/NavTabs.razor` :
```razor
@namespace Piscine.Components.Components.Layout
@using Piscine.Components.Navigation
@inject NavigationManager Nav

@foreach (var d in NavDestinations.Primary)
{
    <a class="nav-tab @(NavDestinations.IsActive(d, Relative) ? "active" : "")"
       href="@d.Route"
       data-testid="@d.TestId">@d.Label</a>
}

@code {
    private string Relative => Nav.ToBaseRelativePath(Nav.Uri);
}
```

`src/Piscine.Components/Components/Layout/NavTabs.razor.css` :
```css
.nav-tab {
    color: var(--nav-fg, inherit);
    text-decoration: none;
    padding: 0.25rem 0.6rem;
    border-radius: 6px;
    font-size: 0.95rem;
}

.nav-tab:hover {
    background: var(--nav-hover-bg, rgba(127, 127, 127, 0.12));
}

.nav-tab.active {
    color: var(--accent, #2563eb);
    font-weight: 500;
}
```

- [ ] **Step 4 : Lancer le test pour le voir passer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~NavTabsTests"`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```
git add src/Piscine.Components/Components/Layout/NavTabs.razor src/Piscine.Components/Components/Layout/NavTabs.razor.css tests/Piscine.Components.Tests/NavTabsTests.cs
git commit -m "feat(qol/s0): composant NavTabs (onglets de destinations)"
```

---

## Tâche 4 : Brancher `NavTabs` dans `MainLayout`

**Files:**
- Modify: `src/Piscine.Components/Components/Layout/MainLayout.razor`

- [ ] **Step 1 : Remplacer le lien « Curriculum » par `<NavTabs />`**

Dans `src/Piscine.Components/Components/Layout/MainLayout.razor`, remplacer le bloc `<nav class="navbar-links">` :

Remplacer :
```razor
        <nav class="navbar-links">
            <a href="/">Curriculum</a>
            <a href="https://github.com/Benjamin-Curlier/piscine-dotnet" target="_blank" rel="noopener">GitHub</a>
            <button id="theme-toggle" type="button" class="theme-toggle"
                    aria-label="Basculer le thème clair/sombre" onclick="toggleTheme()">☾</button>
        </nav>
```
Par :
```razor
        <nav class="navbar-links">
            <NavTabs />
            <a href="https://github.com/Benjamin-Curlier/piscine-dotnet" target="_blank" rel="noopener">GitHub</a>
            <button id="theme-toggle" type="button" class="theme-toggle"
                    aria-label="Basculer le thème clair/sombre" onclick="toggleTheme()">☾</button>
        </nav>
```
(`NavTabs` est dans le même namespace `Piscine.Components.Components.Layout` que `MainLayout` → pas de `@using` à ajouter, comme pour `<NavMenu />`.)

- [ ] **Step 2 : Construire pour vérifier**

Run: `dotnet build src/Piscine.Components -c Release`
Expected : `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3 : Lancer les tests bUnit existants (non-régression)**

Run: `dotnet test tests/Piscine.Components.Tests -c Release`
Expected : PASS (les tests existants + NavTabs + NavDestinations). (Le rendu réel dans la coquille est couvert par l'E2E en Tâche 11.)

- [ ] **Step 4 : Commit**

```
git add src/Piscine.Components/Components/Layout/MainLayout.razor
git commit -m "feat(qol/s0): MainLayout rend les onglets de destinations (fin du lien Curriculum)"
```

---

## Tâche 5 : Rollup de statut d'un module (`ProgressRollup`)

**Files:**
- Create: `src/Piscine.App/Progress/ProgressRollup.cs`
- Test: `tests/Piscine.App.Tests/ProgressRollupTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.App.Tests/ProgressRollupTests.cs` :
```csharp
using Piscine.App.Progress;
using Xunit;

namespace Piscine.App.Tests;

public sealed class ProgressRollupTests
{
    [Fact]
    public void Empty_module_is_NonCommence() =>
        Assert.Equal(ExerciseProgressStatus.NonCommence, ProgressRollup.ForModule([]));

    [Fact]
    public void Any_ARevoir_wins_over_everything()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.ARevoir, ExerciseProgressStatus.EnCours];
        Assert.Equal(ExerciseProgressStatus.ARevoir, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void All_PousseNote_is_complete()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.PousseNote];
        Assert.Equal(ExerciseProgressStatus.PousseNote, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void Partial_progress_is_EnCours()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.PousseNote, ExerciseProgressStatus.NonCommence];
        Assert.Equal(ExerciseProgressStatus.EnCours, ProgressRollup.ForModule(statuses));
    }

    [Fact]
    public void All_NonCommence_stays_NonCommence()
    {
        ExerciseProgressStatus[] statuses =
            [ExerciseProgressStatus.NonCommence, ExerciseProgressStatus.NonCommence];
        Assert.Equal(ExerciseProgressStatus.NonCommence, ProgressRollup.ForModule(statuses));
    }
}
```

- [ ] **Step 2 : Lancer le test pour le voir échouer**

Run: `dotnet test tests/Piscine.App.Tests -c Release --filter "FullyQualifiedName~ProgressRollupTests"`
Expected : ÉCHEC de compilation (`ProgressRollup` n'existe pas).

- [ ] **Step 3 : Implémenter la fonction pure**

`src/Piscine.App/Progress/ProgressRollup.cs` :
```csharp
using System.Collections.Generic;
using System.Linq;

namespace Piscine.App.Progress;

/// <summary>
/// Statut représentatif d'un module à partir des statuts de ses exercices. Pur et déterministe.
/// Priorité : un exo à revoir prime ; sinon « tous poussés/notés » = complet ; sinon tout travail
/// entamé = en cours ; sinon non commencé.
/// </summary>
public static class ProgressRollup
{
    public static ExerciseProgressStatus ForModule(IReadOnlyList<ExerciseProgressStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return ExerciseProgressStatus.NonCommence;
        }

        if (statuses.Any(s => s == ExerciseProgressStatus.ARevoir))
        {
            return ExerciseProgressStatus.ARevoir;
        }

        if (statuses.All(s => s == ExerciseProgressStatus.PousseNote))
        {
            return ExerciseProgressStatus.PousseNote;
        }

        var anyStarted = statuses.Any(s =>
            s is ExerciseProgressStatus.EnCours
              or ExerciseProgressStatus.CommiteNonPousse
              or ExerciseProgressStatus.PousseNote);

        return anyStarted ? ExerciseProgressStatus.EnCours : ExerciseProgressStatus.NonCommence;
    }
}
```

- [ ] **Step 4 : Lancer le test pour le voir passer**

Run: `dotnet test tests/Piscine.App.Tests -c Release --filter "FullyQualifiedName~ProgressRollupTests"`
Expected : PASS (5 tests).

- [ ] **Step 5 : Commit**

```
git add src/Piscine.App/Progress/ProgressRollup.cs tests/Piscine.App.Tests/ProgressRollupTests.cs
git commit -m "feat(qol/s0): rollup de statut par module (ProgressRollup)"
```

---

## Tâche 6 : Libellé/suffixe CSS partagés (DRY) + refactor `StatusBadge`

**Files:**
- Create: `src/Piscine.Components/Components/Progress/ExerciseProgressStatusText.cs`
- Modify: `src/Piscine.Components/Components/Progress/StatusBadge.razor`
- Test: `tests/Piscine.Components.Tests/ExerciseProgressStatusTextTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Components.Tests/ExerciseProgressStatusTextTests.cs` :
```csharp
using Piscine.App.Progress;
using Piscine.Components.Components.Progress;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class ExerciseProgressStatusTextTests
{
    [Theory]
    [InlineData(ExerciseProgressStatus.NonCommence, "Non commencé", "non-commence")]
    [InlineData(ExerciseProgressStatus.EnCours, "En cours", "en-cours")]
    [InlineData(ExerciseProgressStatus.CommiteNonPousse, "Commité, non poussé", "commite-non-pousse")]
    [InlineData(ExerciseProgressStatus.PousseNote, "Poussé → noté", "pousse-note")]
    [InlineData(ExerciseProgressStatus.ARevoir, "À revoir", "a-revoir")]
    public void Label_and_suffix_match_each_status(ExerciseProgressStatus status, string label, string suffix)
    {
        Assert.Equal(label, ExerciseProgressStatusText.Label(status));
        Assert.Equal(suffix, ExerciseProgressStatusText.CssSuffix(status));
    }
}
```

- [ ] **Step 2 : Lancer le test pour le voir échouer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~ExerciseProgressStatusTextTests"`
Expected : ÉCHEC de compilation (`ExerciseProgressStatusText` n'existe pas).

- [ ] **Step 3 : Implémenter le helper partagé**

`src/Piscine.Components/Components/Progress/ExerciseProgressStatusText.cs` :
```csharp
using Piscine.App.Progress;

namespace Piscine.Components.Components.Progress;

/// <summary>Libellé FR et suffixe de classe CSS partagés par StatusBadge et StatusDot (DRY).</summary>
public static class ExerciseProgressStatusText
{
    public static string Label(ExerciseProgressStatus status) => status switch
    {
        ExerciseProgressStatus.NonCommence => "Non commencé",
        ExerciseProgressStatus.EnCours => "En cours",
        ExerciseProgressStatus.CommiteNonPousse => "Commité, non poussé",
        ExerciseProgressStatus.PousseNote => "Poussé → noté",
        ExerciseProgressStatus.ARevoir => "À revoir",
        _ => status.ToString(),
    };

    public static string CssSuffix(ExerciseProgressStatus status) => status switch
    {
        ExerciseProgressStatus.NonCommence => "non-commence",
        ExerciseProgressStatus.EnCours => "en-cours",
        ExerciseProgressStatus.CommiteNonPousse => "commite-non-pousse",
        ExerciseProgressStatus.PousseNote => "pousse-note",
        ExerciseProgressStatus.ARevoir => "a-revoir",
        _ => "unknown",
    };
}
```

- [ ] **Step 4 : Refactorer `StatusBadge` pour réutiliser le helper**

Dans `src/Piscine.Components/Components/Progress/StatusBadge.razor`, remplacer le bloc `@code` par :
```razor
@code {
    [Parameter] public ExerciseProgressStatus Status { get; set; }
    [Parameter] public StatusSource Source { get; set; }

    private string Label => ExerciseProgressStatusText.Label(Status);

    private string CssClass => ExerciseProgressStatusText.CssSuffix(Status);

    private string Title => Source == StatusSource.GitDerived
        ? "Statut déduit de l'état git (best-effort)"
        : string.Empty;
}
```
(Le markup et les classes CSS scopées `status-<suffixe>` restent identiques → `StatusBadge.razor.css` inchangé.)

- [ ] **Step 5 : Lancer les tests (helper + StatusBadge non-régression)**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~ExerciseProgressStatusTextTests|FullyQualifiedName~StatusBadgeTests"`
Expected : PASS (helper 5 cas + StatusBadge inchangés au vert).

- [ ] **Step 6 : Commit**

```
git add src/Piscine.Components/Components/Progress/ExerciseProgressStatusText.cs src/Piscine.Components/Components/Progress/StatusBadge.razor tests/Piscine.Components.Tests/ExerciseProgressStatusTextTests.cs
git commit -m "refactor(qol/s0): libellé/suffixe de statut partagés (DRY StatusBadge)"
```

---

## Tâche 7 : Composant `StatusDot` (pastille compacte)

**Files:**
- Create: `src/Piscine.Components/Components/Progress/StatusDot.razor`
- Create: `src/Piscine.Components/Components/Progress/StatusDot.razor.css`
- Test: `tests/Piscine.Components.Tests/StatusDotTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Components.Tests/StatusDotTests.cs` :
```csharp
using Bunit;
using Piscine.App.Progress;
using Piscine.Components.Components.Progress;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class StatusDotTests : BunitContext
{
    [Theory]
    [InlineData(ExerciseProgressStatus.NonCommence, "non-commence", "Non commencé")]
    [InlineData(ExerciseProgressStatus.EnCours, "en-cours", "En cours")]
    [InlineData(ExerciseProgressStatus.CommiteNonPousse, "commite-non-pousse", "Commité, non poussé")]
    [InlineData(ExerciseProgressStatus.PousseNote, "pousse-note", "Poussé → noté")]
    [InlineData(ExerciseProgressStatus.ARevoir, "a-revoir", "À revoir")]
    public void Render_status_sets_class_data_and_aria(
        ExerciseProgressStatus status, string suffix, string label)
    {
        var cut = Render<StatusDot>(p => p.Add(c => c.Status, status));

        var dot = cut.Find("[data-testid='status-dot']");
        Assert.Contains($"status-{suffix}", dot.GetAttribute("class"), System.StringComparison.Ordinal);
        Assert.Equal(status.ToString(), dot.GetAttribute("data-status"));
        Assert.Equal(label, dot.GetAttribute("aria-label"));
        Assert.Equal(label, dot.GetAttribute("title"));
    }
}
```

- [ ] **Step 2 : Lancer le test pour le voir échouer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~StatusDotTests"`
Expected : ÉCHEC de compilation (`StatusDot` n'existe pas).

- [ ] **Step 3 : Implémenter le composant**

`src/Piscine.Components/Components/Progress/StatusDot.razor` :
```razor
@namespace Piscine.Components.Components.Progress
@using Piscine.App.Progress

<span class="status-dot status-@Suffix"
      data-testid="status-dot"
      data-status="@Status"
      aria-label="@Label"
      title="@Label"></span>

@code {
    [Parameter] public ExerciseProgressStatus Status { get; set; }

    private string Suffix => ExerciseProgressStatusText.CssSuffix(Status);

    private string Label => ExerciseProgressStatusText.Label(Status);
}
```

`src/Piscine.Components/Components/Progress/StatusDot.razor.css` :
```css
.status-dot {
    display: inline-block;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    margin-right: 6px;
    vertical-align: middle;
    flex: none;
    background-color: var(--badge-neutral-bg, #e5e7eb);
}

.status-dot.status-en-cours { background-color: var(--badge-info-bg, #3b82f6); }
.status-dot.status-commite-non-pousse { background-color: var(--badge-warn-bg, #f59e0b); }
.status-dot.status-pousse-note { background-color: var(--badge-success-bg, #10b981); }
.status-dot.status-a-revoir { background-color: var(--badge-error-bg, #ef4444); }
```

- [ ] **Step 4 : Lancer le test pour le voir passer**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~StatusDotTests"`
Expected : PASS (5 cas).

- [ ] **Step 5 : Commit**

```
git add src/Piscine.Components/Components/Progress/StatusDot.razor src/Piscine.Components/Components/Progress/StatusDot.razor.css tests/Piscine.Components.Tests/StatusDotTests.cs
git commit -m "feat(qol/s0): composant StatusDot (pastille de statut)"
```

---

## Tâche 8 : Routage — `Dashboard` sur `/`, `Cours` sur `/cours`

**Files:**
- Create: `src/Piscine.Components/Components/Pages/Dashboard.razor`
- Create: `src/Piscine.Components/Components/Pages/Cours.razor`
- Delete: `src/Piscine.Components/Components/Pages/Home.razor`

- [ ] **Step 1 : Créer le catalogue `Cours.razor` (ex-`Home`, sans le langage console)**

`src/Piscine.Components/Components/Pages/Cours.razor` :
```razor
@page "/cours"
@inject CourseCatalog Catalog

<PageTitle>Piscine .NET — Cours</PageTitle>

<section class="hero">
    <h1>Cours <span class="accent">.NET</span></h1>
    <p class="hero-sub">
        Bootcamp intensif C# / .NET 10. @Catalog.Modules.Count modules,
        @Catalog.Modules.Sum(m => m.ExerciseCount) exercices auto-corrigés.
        Parcours le cours module par module ; ouvre un exercice pour commencer.
    </p>
</section>

<section class="module-grid" data-testid="module-grid">
    @foreach (var m in Catalog.Modules)
    {
        <a class="module-card" href="/module/@m.Id">
            <div class="module-card-num">@m.Number</div>
            <div class="module-card-body">
                <h3>@m.Title</h3>
                <p>
                    @if (m.HasExercises)
                    {
                        @($"{m.ExerciseCount} exercice{(m.ExerciseCount > 1 ? "s" : "")}")
                    }
                    else
                    {
                        <span>Lecture guidée</span>
                    }
                </p>
            </div>
        </a>
    }
</section>
```

- [ ] **Step 2 : Créer le stub `Dashboard.razor` sur `/`**

`src/Piscine.Components/Components/Pages/Dashboard.razor` :
```razor
@page "/"
@inject CourseCatalog Catalog

<PageTitle>Piscine .NET — Tableau de bord</PageTitle>

<section class="hero" data-testid="dashboard">
    <h1>Tableau de bord</h1>
    <p class="hero-sub">
        Bienvenue. @Catalog.Modules.Count modules t'attendent.
        Le tableau de bord détaillé (reprise, progression globale, résultats récents) arrive au sprint S1.
    </p>
</section>

<section class="module-grid">
    <a class="module-card" href="/cours">
        <div class="module-card-body"><h3>Cours</h3><p>Parcourir les modules</p></div>
    </a>
    <a class="module-card" href="/progress">
        <div class="module-card-body"><h3>Progression</h3><p>Voir où tu en es</p></div>
    </a>
    <a class="module-card" href="/terminal">
        <div class="module-card-body"><h3>Terminal</h3><p>Rendre ton travail (git push)</p></div>
    </a>
</section>
```

- [ ] **Step 3 : Supprimer l'ancienne page `Home.razor`**

Run: `git rm src/Piscine.Components/Components/Pages/Home.razor`
Expected : `rm 'src/Piscine.Components/Components/Pages/Home.razor'`.

- [ ] **Step 4 : Construire (deux `@page "/"` en conflit ?)**

Run: `dotnet build src/Piscine.Components -c Release`
Expected : `Build succeeded. 0 Warning(s) 0 Error(s)`. (Si « ambiguous route `/` » : vérifier que `Home.razor` a bien été supprimé.)

- [ ] **Step 5 : Commit**

```
git add src/Piscine.Components/Components/Pages/Dashboard.razor src/Piscine.Components/Components/Pages/Cours.razor
git commit -m "feat(qol/s0): routage / -> tableau de bord (stub) et /cours -> catalogue"
```

---

## Tâche 9 : `NavMenu` — sidebar = arbre des cours + pastilles

**Files:**
- Modify: `src/Piscine.Components/Components/Layout/NavMenu.razor`

- [ ] **Step 1 : Remplacer NavMenu (retrait des liens plats, ajout des pastilles)**

Remplacer **tout** le contenu de `src/Piscine.Components/Components/Layout/NavMenu.razor` par :
```razor
@inject CourseCatalog Catalog
@inject NavigationManager Nav
@inject Piscine.App.Progress.ProgressService Progress
@using Piscine.App.Progress
@using Piscine.Components.Components.Progress

<div class="sidebar-inner">
    <div class="side-section-title">Modules</div>
    <ul class="module-list">
        @foreach (var m in Catalog.Modules)
        {
            var active = IsActiveModule(m.Id);
            <li>
                <details open="@active">
                    <summary>
                        <a class="module-link @(active ? "active" : "")" href="/module/@m.Id">
                            @if (m.HasExercises)
                            {
                                <StatusDot Status="ModuleStatus(m)" />
                            }
                            <span class="mod-num">@m.Number</span>
                            <span class="mod-title">@m.Title</span>
                        </a>
                    </summary>
                    @if (m.HasExercises)
                    {
                        <ul class="exercise-list">
                            @foreach (var ex in m.Groups.SelectMany(g => g.Exercises))
                            {
                                <li>
                                    <a class="exercise-link @(IsActiveExercise(m.Id, ex.Id) ? "active" : "")"
                                       href="/module/@m.Id/@ex.Id">
                                        <StatusDot Status="ExoStatus(m.Id, ex.Id)" />
                                        @ex.Title
                                        @if (ex.Bonus)
                                        {
                                            <span class="badge badge-bonus">bonus</span>
                                        }
                                    </a>
                                </li>
                            }
                        </ul>
                    }
                </details>
            </li>
        }
    </ul>
</div>

@code {
    private readonly Dictionary<(string Module, string Exercise), ExerciseProgressStatus> _status = new();

    protected override void OnInitialized()
    {
        var exos = Catalog.Modules
            .SelectMany(m => m.Groups.SelectMany(g => g.Exercises))
            .Select(ex => (ex.ModuleId, ex.Id));

        foreach (var info in Progress.SnapshotFor(exos))
        {
            _status[(info.ModuleId, info.ExerciseId)] = info.Status;
        }
    }

    private ExerciseProgressStatus ExoStatus(string moduleId, string exerciseId)
        => _status.GetValueOrDefault((moduleId, exerciseId), ExerciseProgressStatus.NonCommence);

    private ExerciseProgressStatus ModuleStatus(CourseModule m)
        => ProgressRollup.ForModule(
            m.Groups.SelectMany(g => g.Exercises)
                .Select(ex => ExoStatus(m.Id, ex.Id))
                .ToList());

    private string[] Segments => Nav
        .ToBaseRelativePath(Nav.Uri)
        .Split('?', '#')[0]
        .Split('/', StringSplitOptions.RemoveEmptyEntries);

    private bool IsActiveModule(string moduleId)
        => Segments.Length >= 2
           && Segments[0] == "module"
           && string.Equals(Segments[1], moduleId, StringComparison.OrdinalIgnoreCase);

    private bool IsActiveExercise(string moduleId, string exerciseId)
        => IsActiveModule(moduleId)
           && Segments.Length >= 3
           && string.Equals(Segments[2], exerciseId, StringComparison.OrdinalIgnoreCase);
}
```
(`CourseModule` vient de `Piscine.Components.Services` — déjà importé par l'`_Imports.razor` de la RCL, comme l'usage actuel de `Catalog.Modules`. `ProgressService` est enregistré dans la DI du DevHost ET de Photino — vérifié dans `Program.cs` des deux hôtes.)

- [ ] **Step 2 : Construire pour vérifier**

Run: `dotnet build src/Piscine.Components -c Release`
Expected : `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3 : Lancer toute la suite bUnit (non-régression composants)**

Run: `dotnet test tests/Piscine.Components.Tests -c Release`
Expected : PASS. (L'intégration NavMenu↔ProgressService — qui lit git — est couverte par l'E2E en Tâche 11.)

- [ ] **Step 4 : Commit**

```
git add src/Piscine.Components/Components/Layout/NavMenu.razor
git commit -m "feat(qol/s0): sidebar = arbre des cours avec pastilles de statut"
```

---

## Tâche 10 : E2E — routage + pastilles (DevHost + Playwright)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/NavigationSmokeTests.cs`
- Modify: `tests/Piscine.DevHost.E2E/SmokeTests.cs`

- [ ] **Step 1 : Écrire l'E2E de navigation**

`tests/Piscine.DevHost.E2E/NavigationSmokeTests.cs` :
```csharp
using System.Diagnostics;
using Microsoft.Playwright;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.DevHost.E2E;

/// <summary>
/// Smoke E2E de la fondation de nav S0 : démarre le DevHost avec un progress.json planté
/// (ex00-hello en ARevoir), puis vérifie que (1) « / » rend le tableau de bord
/// (data-testid="dashboard"), (2) « /cours » rend la grille de modules (data-testid="module-grid"),
/// (3) la sidebar porte des pastilles (data-testid="status-dot") dont une en data-status="ARevoir",
/// (4) l'onglet primaire « nav-dashboard » pointe vers « / ». Skip propre sans Chromium.
/// Port dédié 5257 (distinct de 5247/5249/5251/5253/5255).
/// </summary>
public sealed class NavigationSmokeTests : IAsyncLifetime
{
    private const int Port = 5257;
    private static readonly string BaseUrl = $"http://localhost:{Port}";

    private Process? _host;
    private string? _tempHome;
    private string? _tempWorkspace;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var devHostProject = Path.Combine(repoRoot, "src", "Piscine.DevHost");
        var contentDir = Path.Combine(repoRoot, "content");

        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-e2e-nav-{Guid.NewGuid():N}");
        _tempWorkspace = Path.Combine(_tempHome, "workspace");
        var stateDir = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(_tempWorkspace);

        var progressPath = Path.Combine(stateDir, "progress.json");
        var progress = new Progress();
        progress.Exercises["ex00-hello"] = new ExerciseProgress { Status = ExerciseStatus.ARevoir, Attempts = 1 };
        new ProgressStore(progressPath).Save(progress);

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = $"run --project \"{devHostProject}\" --urls {BaseUrl}",
            UseShellExecute = false,
            WorkingDirectory = repoRoot,
        };
        psi.EnvironmentVariables["PISCINE_CONTENT"] = contentDir;
        psi.EnvironmentVariables["PISCINE_WORKSPACE"] = _tempWorkspace;
        psi.EnvironmentVariables["PISCINE_HOME"] = _tempHome;

        _host = Process.Start(psi)
            ?? throw new InvalidOperationException("Impossible de démarrer le DevHost.");

        await WaitForServerAsync(TimeSpan.FromSeconds(90));
    }

    public Task DisposeAsync()
    {
        if (_host is { HasExited: false })
        {
            try { _host.Kill(entireProcessTree: true); }
            catch { /* déjà mort */ }
        }
        _host?.Dispose();

        if (_tempHome is not null && Directory.Exists(_tempHome))
        {
            try { Directory.Delete(_tempHome, recursive: true); }
            catch { /* pas critique */ }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Root_is_dashboard_and_cours_is_catalogue_with_status_dots()
    {
        using var pw = await Playwright.CreateAsync();

        IBrowser browser;
        try
        {
            browser = await pw.Chromium.LaunchAsync();
        }
        catch (PlaywrightException)
        {
            return; // Chromium absent (CI sans playwright install) : skip propre.
        }

        await using (browser)
        {
            var page = await browser.NewPageAsync();

            // (1) « / » = tableau de bord.
            await page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='dashboard']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // (4) onglet primaire vers « / ».
            var dashHref = await page.Locator("[data-testid='nav-dashboard']").First.GetAttributeAsync("href");
            Assert.Equal("/", dashHref);

            // (2) « /cours » = grille de modules.
            await page.GotoAsync($"{BaseUrl}/cours", new PageGotoOptions { Timeout = 30_000 });
            await page.WaitForSelectorAsync("[data-testid='module-grid']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            // (3) la sidebar porte des pastilles, dont une ARevoir (ex00-hello planté).
            var dots = await page.Locator("[data-testid='status-dot']").CountAsync();
            Assert.True(dots > 0, "Aucune pastille data-testid='status-dot' dans la sidebar.");

            var aRevoir = await page.Locator("[data-testid='status-dot'][data-status='ARevoir']").CountAsync();
            Assert.True(aRevoir > 0, $"Aucune pastille ARevoir trouvée (pastilles totales : {dots}).");
        }
    }

    private static async Task WaitForServerAsync(TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await http.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException) { /* pas prêt */ }
            catch (TaskCanceledException) { /* retente */ }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Le DevHost n'a pas répondu sur {BaseUrl} ({timeout.TotalSeconds:0}s).");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Racine du dépôt introuvable (Piscine.slnx absent).");
    }
}
```

- [ ] **Step 2 : Mettre à jour `SmokeTests` (le catalogue est sur `/cours`)**

Dans `tests/Piscine.DevHost.E2E/SmokeTests.cs`, méthode `DevHost_renders_a_course`, remplacer le bloc de navigation/assertion :

Remplacer :
```csharp
            var page = await browser.NewPageAsync();
            await page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 30_000 });

            // Un cours rendu expose au moins un titre.
            await page.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30_000 });

            Assert.Contains("Piscine", await page.TitleAsync());
```
Par :
```csharp
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/cours", new PageGotoOptions { Timeout = 30_000 });

            // Le catalogue de cours expose la grille de modules + un titre.
            await page.WaitForSelectorAsync("[data-testid='module-grid']", new PageWaitForSelectorOptions { Timeout = 30_000 });

            Assert.Contains("Piscine", await page.TitleAsync());
```

- [ ] **Step 3 : Installer Chromium puis lancer les E2E**

Run (si pas déjà fait) : `pwsh tests/Piscine.DevHost.E2E/bin/Release/net10.0/playwright.ps1 install chromium`
Run: `dotnet test tests/Piscine.DevHost.E2E -c Release --filter "FullyQualifiedName~NavigationSmokeTests|FullyQualifiedName~SmokeTests"`
Expected : PASS (avec Chromium) ; sans Chromium, les tests se sautent proprement (toujours vert).

- [ ] **Step 4 : Commit**

```
git add tests/Piscine.DevHost.E2E/NavigationSmokeTests.cs tests/Piscine.DevHost.E2E/SmokeTests.cs
git commit -m "test(qol/s0): E2E routage tableau de bord/cours + pastilles sidebar"
```

---

## Tâche 11 : Vérification finale + smoke Photino

**Files:** (aucun nouveau ; vérification + éventuel commit de finition)

- [ ] **Step 1 : Suite complète au vert**

Run: `dotnet test Piscine.slnx -c Release`
Expected : tout PASS, **0 warning** (build inclus). (Référence d'avant S0 : 305 tests ; S0 ajoute ~17 tests unit/bUnit + 1 E2E.)

- [ ] **Step 2 : Gate de contenu (intacte)**

Run: `$env:PISCINE_CONTENT = "$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content`
Expected : `Contenu valide.` (S0 ne touche pas au contenu ; garde anti-régression.)

- [ ] **Step 3 : Smoke Photino (fenêtre native — action manuelle)**

Run: `dotnet run --project src/Piscine.Desktop -c Release`
Expected : la fenêtre native s'ouvre sur le **tableau de bord** (`<h1>Tableau de bord</h1>`), les **onglets**
en haut listent Tableau de bord / Cours / Progression / Vérifier / Initialiser / Résultat / Terminal,
la **sidebar** affiche l'arbre des modules avec des **pastilles** ; cliquer « Cours » montre la grille ;
0 exception au démarrage. (Fenêtre native non vérifiable par un agent → smoke visuel proprio.)

- [ ] **Step 4 : Marquer le plan terminé**

Cocher toutes les cases de ce plan. (Le retex de sprint + l'éventuelle PR sont laissés au proprio,
conformément au rythme du dépôt ; ce plan ne pousse pas.)

---

## Notes de revue (auto-revue du plan)

- **Couverture spec (S0)** : destinations-en-données + `IsActive` (T2) ; fusion nav redondante /
  onglets (T3/T4) ; routage `/`→board, `/cours`→catalogue (T8) ; pastilles sidebar (T5/T6/T7/T9) ;
  audit UX (T1). ✔
- **Invariants** : aucun fichier de `Piscine.Core`/`Piscine.Grading`/`Piscine.Git`/`Piscine.Cli`,
  ni `release.yml`, n'est modifié ; seuls la RCL, `Piscine.App` (ajout pur `ProgressRollup`) et les
  tests changent. ✔
- **Cohérence des types** : `ExerciseProgressStatusText.{Label,CssSuffix}` utilisés à l'identique par
  `StatusBadge` (T6) et `StatusDot` (T7) ; `ProgressRollup.ForModule(IReadOnlyList<…>)` appelé en
  `.ToList()` depuis `NavMenu` (T9) ; `NavDestinations.IsActive(dest, relativePath)` même signature
  en T2/T3. ✔
- **Risque E2E** : seul `SmokeTests` ciblait `/` (mis à jour en T10) ; aucun autre test ne dépend du
  catalogue sur `/` (vérifié par recherche `module-card`/`Curriculum`/`nav-*`). ✔
- **Hors S0 (sprints suivants)** : carte « Reprendre » + progression globale (S1) ; barre d'action
  Ouvrir/Vérifier (S2) ; rafraîchissement live des pastilles après push/check (S4). Le stub Dashboard
  est volontairement minimal.
