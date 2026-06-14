# Chrome de fenêtre personnalisé (façon Discord) — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retirer le chrome OS de l'app de bureau Photino et intégrer les contrôles de fenêtre (réduire / agrandir-restaurer / fermer) + le déplacement dans la navbar existante (barre unique draggable), avec parité navigateur (DevHost) et repli OS-chrome sous Linux si nécessaire.

**Architecture:** UI dans la RCL `Piscine.Components` (composant `WindowControls` + module `windowChrome.js` + CSS), pilotage natif dans l'hôte `Piscine.Desktop` (fenêtre `Chromeless` + handler de messages `PISCINE_WIN:*` → API fenêtre Photino). Le DevHost (navigateur) ne détecte aucun hôte natif → contrôles masqués, drag inerte. Pont JS↔hôte = `window.external.sendMessage` (déjà utilisé par `SmokeProbe`) côté page, `RegisterWebMessageReceivedHandler` / `SendWebMessage` côté hôte.

**Tech Stack:** .NET 10, Blazor (RCL bi-hôte), PhotinoX.Blazor 4.2.0, JS interop par web messages, xUnit + bUnit + Playwright.

**Spec:** `docs/superpowers/specs/2026-06-14-custom-window-chrome-design.md`

**Invariants:** Ne PAS toucher `src/Piscine.Core`, `Piscine.Grading`, `Piscine.Git`, `Piscine.GitShim`, `Piscine.Cli`, `Piscine.Sandbox*`, ni la logique de `.github/release.yml`. Commits conventionnels FR + trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`. commit ≠ push. Branche : `feat/custom-window-chrome` (déjà créée).

---

## Fichiers

| Fichier | Rôle |
|---|---|
| `src/Piscine.Components/wwwroot/js/windowChrome.js` | **Créer.** Détection hôte, `winControl(action)`, drag/double-clic, écoute d'état. Inerte en navigateur. |
| `src/Piscine.Components/Components/Layout/WindowControls.razor` (+`.razor.css`) | **Créer.** 3 boutons fenêtre (aria FR), `onclick="winControl(...)"`. |
| `src/Piscine.Components/Components/Layout/MainLayout.razor` | **Modifier.** Monter `<WindowControls/>`, marquer zone drag / `no-drag`. |
| `src/Piscine.Components/wwwroot/css/piscine.css` | **Modifier.** Styles barre de titre, contrôles, coins/ombre, `is-maximized`, masquage navigateur. |
| `src/Piscine.Desktop/wwwroot/index.html` | **Modifier.** Charger `windowChrome.js`. |
| `src/Piscine.DevHost/Components/App.razor` | **Modifier.** Charger `windowChrome.js` (inerte). |
| `src/Piscine.Desktop/WindowChromeHost.cs` | **Créer.** Handler `PISCINE_WIN:*` → API fenêtre Photino + annonce d'état. |
| `src/Piscine.Desktop/Program.cs` | **Modifier.** `SetChromeless` (gardé OS) + brancher `WindowChromeHost`. |
| `tests/Piscine.Components.Tests/WindowControlsTests.cs` | **Créer.** bUnit. |
| `tests/Piscine.DevHost.E2E/WindowChromeSmokeTests.cs` | **Créer.** Playwright, port **5281**. |

---

## Task 1: Composant `WindowControls` (bUnit)

**Files:**
- Create: `src/Piscine.Components/Components/Layout/WindowControls.razor`
- Create: `src/Piscine.Components/Components/Layout/WindowControls.razor.css`
- Test: `tests/Piscine.Components.Tests/WindowControlsTests.cs`

- [ ] **Step 1: Écrire le test bUnit (échoue)**

```csharp
using Bunit;
using Piscine.Components.Components.Layout;
using Xunit;

namespace Piscine.Components.Tests;

public class WindowControlsTests : BunitContext
{
    [Fact]
    public void Renders_three_window_buttons_with_french_aria_labels()
    {
        var cut = Render<WindowControls>();
        var buttons = cut.FindAll("button.win-btn");
        Assert.Equal(3, buttons.Count);
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Réduire la fenêtre");
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Agrandir ou restaurer la fenêtre");
        Assert.Contains(buttons, b => b.GetAttribute("aria-label") == "Fermer la fenêtre");
    }

    [Fact]
    public void Buttons_invoke_winControl_via_onclick_attribute()
    {
        var cut = Render<WindowControls>();
        var markup = cut.Markup;
        Assert.Contains("winControl('minimize')", markup);
        Assert.Contains("winControl('togglemax')", markup);
        Assert.Contains("winControl('close')", markup);
    }
}
```

- [ ] **Step 2: Lancer le test → échec** (le type `WindowControls` n'existe pas).

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~WindowControlsTests"`
Expected: FAIL (compilation : `WindowControls` introuvable).

- [ ] **Step 3: Créer `WindowControls.razor`**

```razor
@* Contrôles de fenêtre (barre de titre custom). Boutons HTML simples appelant le module
   global windowChrome.js (winControl) — même motif que le bouton thème (onclick="toggleTheme()").
   Masqué hors hôte Photino via CSS (html:not([data-host="photino"]) .window-controls). *@
<div class="window-controls" aria-label="Contrôles de la fenêtre">
    <button type="button" class="win-btn win-min" aria-label="Réduire la fenêtre"
            title="Réduire" onclick="winControl('minimize')">
        <svg viewBox="0 0 10 10" width="10" height="10" aria-hidden="true"><rect x="1" y="4.5" width="8" height="1"/></svg>
    </button>
    <button type="button" class="win-btn win-max" aria-label="Agrandir ou restaurer la fenêtre"
            title="Agrandir / Restaurer" onclick="winControl('togglemax')">
        <svg viewBox="0 0 10 10" width="10" height="10" aria-hidden="true"><rect x="1.5" y="1.5" width="7" height="7" fill="none" stroke="currentColor" stroke-width="1"/></svg>
    </button>
    <button type="button" class="win-btn win-close" aria-label="Fermer la fenêtre"
            title="Fermer" onclick="winControl('close')">
        <svg viewBox="0 0 10 10" width="10" height="10" aria-hidden="true"><path d="M1 1 L9 9 M9 1 L1 9" stroke="currentColor" stroke-width="1.2"/></svg>
    </button>
</div>
```

- [ ] **Step 4: Créer `WindowControls.razor.css`**

```css
.window-controls { display: inline-flex; align-items: center; gap: 2px; -webkit-app-region: no-drag; app-region: no-drag; }
.win-btn {
    display: inline-flex; align-items: center; justify-content: center;
    width: 34px; height: 28px; border: 0; background: transparent;
    color: var(--text, #c9d1d9); cursor: pointer; border-radius: 6px;
}
.win-btn:hover { background: var(--surface-hover, rgba(255,255,255,.08)); }
.win-btn:focus-visible { outline: 2px solid var(--accent, #8957e5); outline-offset: -2px; }
.win-close:hover { background: #e81123; color: #fff; }
```

- [ ] **Step 5: Lancer le test → succès**

Run: `dotnet test tests/Piscine.Components.Tests -c Release --filter "FullyQualifiedName~WindowControlsTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/Piscine.Components/Components/Layout/WindowControls.razor src/Piscine.Components/Components/Layout/WindowControls.razor.css tests/Piscine.Components.Tests/WindowControlsTests.cs
git commit -m "feat(chrome): composant WindowControls (3 boutons fenêtre) + bUnit

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Module JS `windowChrome.js` (détection hôte + drag + contrôles)

**Files:**
- Create: `src/Piscine.Components/wwwroot/js/windowChrome.js`

- [ ] **Step 1: Créer `windowChrome.js`**

```javascript
// Chrome de fenêtre custom. Inerte hors hôte Photino (pas de window.external.sendMessage).
(function () {
  var isPhotino = !!(window.external && window.external.sendMessage);
  document.documentElement.setAttribute('data-host', isPhotino ? 'photino' : 'browser');
  window.__isPhotino = isPhotino;

  function send(msg) { try { window.external.sendMessage(msg); } catch (e) { } }

  // Appelé par les boutons (onclick="winControl('minimize'|'togglemax'|'close')").
  window.winControl = function (action) {
    if (!isPhotino) return;
    send('PISCINE_WIN:' + action);
  };

  if (!isPhotino) return; // navigateur : pas de drag, contrôles masqués par CSS.

  // Déplacement : pointerdown sur une zone drag (hors no-drag) → envoie des deltas écran à l'hôte.
  var dragging = false, lastX = 0, lastY = 0;
  function isDragZone(el) {
    for (var n = el; n && n !== document.documentElement; n = n.parentElement) {
      if (n.matches && n.matches('.no-drag, button, a, input, select, textarea, [contenteditable]')) return false;
      if (n.matches && n.matches('.titlebar-drag')) return true;
    }
    return false;
  }
  document.addEventListener('pointerdown', function (e) {
    if (e.button !== 0 || !isDragZone(e.target)) return;
    dragging = true; lastX = e.screenX; lastY = e.screenY;
    try { e.target.setPointerCapture && e.target.setPointerCapture(e.pointerId); } catch (x) { }
  });
  document.addEventListener('pointermove', function (e) {
    if (!dragging) return;
    var dx = e.screenX - lastX, dy = e.screenY - lastY;
    if (dx || dy) { send('PISCINE_WIN:dragby:' + dx + ',' + dy); lastX = e.screenX; lastY = e.screenY; }
  });
  document.addEventListener('pointerup', function () { dragging = false; });
  document.addEventListener('dblclick', function (e) { if (isDragZone(e.target)) send('PISCINE_WIN:togglemax'); });

  // L'hôte annonce l'état agrandi → bascule une classe pour le style (coins/ombre).
  window.__winState = function (state) {
    document.documentElement.classList.toggle('is-maximized', state === 'maximized');
  };
})();
```

- [ ] **Step 2: Vérifier le build de la RCL** (le fichier est un asset statique, copié sous `_content/Piscine.Components/js/`).

Run: `dotnet build src/Piscine.Components -c Release`
Expected: Build succeeded, 0 Warning.

- [ ] **Step 3: Commit**

```bash
git add src/Piscine.Components/wwwroot/js/windowChrome.js
git commit -m "feat(chrome): module windowChrome.js (détection hôte, drag, winControl)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Intégrer dans `MainLayout` + CSS + charger le JS dans les deux hôtes

**Files:**
- Modify: `src/Piscine.Components/Components/Layout/MainLayout.razor`
- Modify: `src/Piscine.Components/wwwroot/css/piscine.css`
- Modify: `src/Piscine.Desktop/wwwroot/index.html`
- Modify: `src/Piscine.DevHost/Components/App.razor`

- [ ] **Step 1: Modifier `MainLayout.razor`** — rendre la navbar draggable, ajouter les contrôles.

Remplacer le bloc `<header class="navbar"> … </header>` par (ajouts : classe `titlebar-drag` sur le header, `no-drag` sur les liens, `<WindowControls/>` à la fin) :

```razor
    <header class="navbar titlebar-drag">
        <a class="brand no-drag" href="/">
            <span class="brand-mark">{ }</span>
            <span class="brand-text">Piscine <strong>.NET</strong></span>
        </a>
        <nav class="navbar-links no-drag" aria-label="Navigation principale">
            <NavTabs />
            <a href="https://github.com/Benjamin-Curlier/piscine-dotnet" target="_blank" rel="noopener">GitHub</a>
            <button id="theme-toggle" type="button" class="theme-toggle"
                    aria-label="Basculer le thème clair/sombre" onclick="toggleTheme()">☾</button>
        </nav>
        <WindowControls />
    </header>
```

- [ ] **Step 2: Ajouter les styles dans `piscine.css`** (bloc commenté en fin de fichier) :

```css
/* ── Chrome de fenêtre custom (chromeless Photino) ────────────────────────── */
/* Zone de drag : seulement sous hôte Photino (sinon clics normaux navigateur). */
html[data-host="photino"] .titlebar-drag { -webkit-app-region: drag; app-region: drag; }
html[data-host="photino"] .titlebar-drag .no-drag,
html[data-host="photino"] .titlebar-drag button,
html[data-host="photino"] .titlebar-drag a { -webkit-app-region: no-drag; app-region: no-drag; }
/* Contrôles de fenêtre : masqués hors Photino (le navigateur / l'OS fournissent le chrome). */
.window-controls { margin-left: 8px; }
html:not([data-host="photino"]) .window-controls { display: none; }
/* Fenêtre chromeless : coins arrondis + ombre, retirés une fois agrandie. */
html[data-host="photino"] body { border-radius: 8px; overflow: hidden; }
html[data-host="photino"].is-maximized body { border-radius: 0; }
```

- [ ] **Step 3: Charger `windowChrome.js` dans l'hôte Photino** — `src/Piscine.Desktop/wwwroot/index.html`, juste après le script `theme.js` (ligne `<script src="_content/Piscine.Components/js/theme.js"></script>`), ajouter :

```html
    <!-- Chrome de fenêtre custom (chromeless) : détection hôte + drag + contrôles. Inerte en navigateur. -->
    <script src="_content/Piscine.Components/js/windowChrome.js"></script>
```

- [ ] **Step 4: Charger `windowChrome.js` dans le DevHost** — `src/Piscine.DevHost/Components/App.razor`, à côté du chargement de `theme.js` (chercher `theme.js`), ajouter la même balise `<script src="_content/Piscine.Components/js/windowChrome.js"></script>`. (En navigateur le module est inerte mais pose `data-host="browser"`.)

- [ ] **Step 5: Build complet**

Run: `dotnet build Piscine.slnx -c Release`
Expected: Build succeeded, 0 Warning, 0 Error.

- [ ] **Step 6: Commit**

```bash
git add src/Piscine.Components/Components/Layout/MainLayout.razor src/Piscine.Components/wwwroot/css/piscine.css src/Piscine.Desktop/wwwroot/index.html src/Piscine.DevHost/Components/App.razor
git commit -m "feat(chrome): navbar = barre de titre (drag + contrôles), chargement JS 2 hôtes

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: E2E DevHost — parité navigateur (Playwright)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/WindowChromeSmokeTests.cs`

- [ ] **Step 1: Écrire le test** (modelé sur `NavigationSmokeTests.cs`, port **5281**, skip-sans-Chromium).

```csharp
using Microsoft.Playwright;
using Xunit;

namespace Piscine.DevHost.E2E;

// En navigateur (DevHost), data-host="browser" → contrôles de fenêtre MASQUÉS, navigation intacte.
// Port dédié 5281 (distinct de 5247/5249/5251/5253/5255/5257/5259/5261/5263/5265/5267/5269/5271).
public sealed class WindowChromeSmokeTests
{
    private const int Port = 5281;

    [Fact]
    public async Task Browser_hides_window_controls_and_keeps_nav()
    {
        await using var app = await DevHostFixture.StartAsync(Port);
        IBrowser browser;
        try { browser = await DevHostFixture.LaunchChromiumAsync(); }
        catch (PlaywrightException) { return; } // pas de navigateur → skip propre
        await using var _ = browser;

        var page = await browser.NewPageAsync();
        await page.GotoAsync($"http://localhost:{Port}/");
        await page.WaitForSelectorAsync("header.navbar");

        var host = await page.EvalOnSelectorAsync<string>("html", "el => el.getAttribute('data-host')");
        Assert.Equal("browser", host);

        var controls = await page.QuerySelectorAsync(".window-controls");
        if (controls is not null)
            Assert.False(await controls.IsVisibleAsync(), "Les contrôles de fenêtre doivent être masqués en navigateur.");

        Assert.NotNull(await page.QuerySelectorAsync("[data-testid='nav-cours']"));
    }
}
```

> NOTE : aligner les helpers (`DevHostFixture.StartAsync` / `LaunchChromiumAsync` ou équivalent) sur le motif réel de `NavigationSmokeTests.cs` du repo. Reprendre la même fixture/serveur que les autres smoke tests.

- [ ] **Step 2: Lancer le test en isolation**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release --filter "FullyQualifiedName~WindowChromeSmokeTests"`
Expected: PASS (ou skip propre sans Chromium).

- [ ] **Step 3: Commit**

```bash
git add tests/Piscine.DevHost.E2E/WindowChromeSmokeTests.cs
git commit -m "test(chrome): E2E parité navigateur — contrôles masqués, nav intacte (port 5281)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: Hôte Photino — chromeless + handler de messages fenêtre

**Files:**
- Create: `src/Piscine.Desktop/WindowChromeHost.cs`
- Modify: `src/Piscine.Desktop/Program.cs`

- [ ] **Step 1: Vérifier la surface d'API PhotinoX/Photino.NET** — ouvrir `PhotinoWindow` (IntelliSense / build d'essai) et confirmer les membres utilisés : `SetChromeless(bool)`, `SetMinimized(bool)`, `SetMaximized(bool)`, `Maximized` (get), `Close()`, position via `Left`/`Top` (get) + `SetLeft(int)`/`SetTop(int)` (ou `SetLocation(Point)`), `SendWebMessage(string)`, `RegisterWebMessageReceivedHandler(...)`. **Adapter les noms ci-dessous si le fork diffère** (ex. propriétés `Minimized=`/`Maximized=` au lieu de `SetX`). C'est une vérif de compilation, pas un blanc de conception.

- [ ] **Step 2: Créer `WindowChromeHost.cs`**

```csharp
using System.Globalization;
using Photino.Blazor;

namespace Piscine.Desktop;

/// <summary>
/// Pilote la fenêtre Photino chromeless depuis la page (web messages "PISCINE_WIN:&lt;action&gt;").
/// Actions : minimize, togglemax, close, dragby:&lt;dx&gt;,&lt;dy&gt;. Annonce l'état agrandi à la page
/// (window.__winState) pour le style. Coexiste avec SmokeProbe (préfixes de message distincts).
/// </summary>
internal static class WindowChromeHost
{
    private const string Prefix = "PISCINE_WIN:";

    public static void Attach(PhotinoBlazorApp app)
    {
        var win = app.MainWindow;
        win.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal)) return;
            var cmd = message[Prefix.Length..];
            try
            {
                if (cmd == "minimize") { win.SetMinimized(true); }
                else if (cmd == "close") { win.Close(); }
                else if (cmd == "togglemax")
                {
                    var max = !win.Maximized;
                    win.SetMaximized(max);
                    win.SendWebMessage("PISCINE_WIN_STATE:" + (max ? "maximized" : "normal"));
                }
                else if (cmd.StartsWith("dragby:", StringComparison.Ordinal))
                {
                    var parts = cmd["dragby:".Length..].Split(',');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dy))
                    {
                        if (win.Maximized) { win.SetMaximized(false); win.SendWebMessage("PISCINE_WIN_STATE:normal"); }
                        win.SetLeft(win.Left + dx);
                        win.SetTop(win.Top + dy);
                    }
                }
            }
            catch { /* best-effort : ne jamais faire planter l'hôte depuis le chrome. */ }
        });
    }
}
```

- [ ] **Step 3: Modifier `Program.cs`** — activer le chromeless (gardé par OS pour le repli Linux) et brancher le handler. Remplacer le bloc :

```csharp
        app.MainWindow
           .SetTitle("Piscine .NET")
           .SetUseOsDefaultSize(true);
```

par :

```csharp
        app.MainWindow
           .SetTitle("Piscine .NET")
           .SetUseOsDefaultSize(true);

        // Chrome custom (barre de titre = navbar). Chromeless sous Windows ; sous Linux (WebKitGTK) le
        // drag piloté JS peut être instable → on garde le chrome OS par défaut (repli, cf. spec §5.2).
        // Le smoke de rendu prouve que le chromeless ne casse pas le rendu (pas d'écran noir).
        if (!OperatingSystem.IsLinux())
        {
            app.MainWindow.SetChromeless(true);
            WindowChromeHost.Attach(app);
        }
```

- [ ] **Step 4: Build complet 0 warning**

Run: `dotnet build Piscine.slnx -c Release`
Expected: Build succeeded, 0 Warning, 0 Error. (Si erreur de compilation sur un nom d'API Photino → corriger selon Step 1.)

- [ ] **Step 5: Smoke de rendu Photino (local Windows) — la fenêtre rend chromeless**

Run (depuis la racine repo) :
```bash
rm -f /tmp/chrome-smoke.json 2>/dev/null; \
PISCINE_SMOKE=1 PISCINE_SMOKE_OUT="$PWD/chrome-smoke.json" PISCINE_CONTENT="$PWD/content" \
  dotnet run --project src/Piscine.Desktop -c Release
```
(En PowerShell : lancer en arrière-plan, lire `chrome-smoke.json`, puis `Stop-Process -Name Piscine.Desktop`.)
Expected: le JSON contient `"received":true` et `appTextLen>0` → rendu OK, **pas d'écran noir**. Vérif visuelle manuelle : **pas de barre de titre OS**, contrôles custom visibles à droite, drag de la navbar déplace la fenêtre, réduire/agrandir/fermer fonctionnent.

- [ ] **Step 6: Commit**

```bash
git add src/Piscine.Desktop/WindowChromeHost.cs src/Piscine.Desktop/Program.cs
git commit -m "feat(chrome): fenêtre chromeless + handler PISCINE_WIN (min/max/close/drag)

Repli OS-chrome sous Linux (drag WebKitGTK potentiellement instable). Smoke de
rendu OK (pas d'écran noir).

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: Redimensionnement (conditionnel — selon Step 5 de Task 5)

**Files:**
- Modify (si nécessaire): `src/Piscine.Components/Components/Layout/MainLayout.razor`, `piscine.css`, `windowChrome.js`, `WindowChromeHost.cs`

- [ ] **Step 1: Évaluer** — pendant la vérif manuelle (Task 5 Step 5), tester si la fenêtre chromeless se **redimensionne** par les bords (l'OS conserve parfois une bordure de resize même chromeless).
  - **Si le resize OS fonctionne** : RAS, cocher et passer à Task 7.
  - **Si le resize est impossible** : ajouter de fines poignées CSS pilotant le resize :

- [ ] **Step 2 (si resize KO): poignées de resize** — ajouter dans `MainLayout` (juste après `<div class="layout">`), masquées hors Photino :

```razor
    <div class="resize-handles" aria-hidden="true">
        <div class="rh rh-e"></div><div class="rh rh-s"></div><div class="rh rh-se"></div>
    </div>
```

CSS (`piscine.css`) :
```css
html:not([data-host="photino"]) .resize-handles { display: none; }
html[data-host="photino"] .resize-handles .rh { position: fixed; z-index: 9999; -webkit-app-region: no-drag; app-region: no-drag; }
.rh-e { top:0; right:0; width:5px; height:100%; cursor:ew-resize; }
.rh-s { left:0; bottom:0; height:5px; width:100%; cursor:ns-resize; }
.rh-se { right:0; bottom:0; width:10px; height:10px; cursor:nwse-resize; }
```

JS (`windowChrome.js`, dans le bloc `isPhotino`) :
```javascript
  // Redimensionnement par poignées : envoie des deltas à l'hôte (resizeby:edge:dx,dy).
  ['e','s','se'].forEach(function (edge) {
    document.addEventListener('pointerdown', function (e) {
      var h = e.target.closest && e.target.closest('.rh-' + edge); if (!h) return;
      var lx = e.screenX, ly = e.screenY; e.preventDefault();
      function mv(ev) { var dx = ev.screenX - lx, dy = ev.screenY - ly; if (dx||dy){ send('PISCINE_WIN:resizeby:'+edge+':'+dx+','+dy); lx=ev.screenX; ly=ev.screenY; } }
      function up() { document.removeEventListener('pointermove', mv); document.removeEventListener('pointerup', up); }
      document.addEventListener('pointermove', mv); document.addEventListener('pointerup', up);
    });
  });
```

Handler (`WindowChromeHost.cs`, ajouter une branche) :
```csharp
                else if (cmd.StartsWith("resizeby:", StringComparison.Ordinal))
                {
                    var rest = cmd["resizeby:".Length..];
                    var c = rest.IndexOf(':'); if (c < 0) return;
                    var edge = rest[..c];
                    var d = rest[(c + 1)..].Split(',');
                    if (d.Length == 2
                        && int.TryParse(d[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
                        && int.TryParse(d[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dy))
                    {
                        if (edge.Contains('e')) { win.SetWidth(Math.Max(640, win.Width + dx)); }
                        if (edge.Contains('s')) { win.SetHeight(Math.Max(480, win.Height + dy)); }
                    }
                }
```

- [ ] **Step 3 (si modifié): build + smoke + commit**

```bash
dotnet build Piscine.slnx -c Release
git add -A
git commit -m "feat(chrome): poignées de redimensionnement (chromeless sans bordure OS)

Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Vérification d'intégration complète

- [ ] **Step 1: Build Release 0 warning**

Run: `dotnet build Piscine.slnx -c Release`
Expected: 0 Warning, 0 Error.

- [ ] **Step 2: Tests non-E2E**

Run: `dotnet test Piscine.slnx -c Release --no-build --filter "FullyQualifiedName!~DevHost.E2E"`
Expected: tous verts (dont `WindowControlsTests`).

- [ ] **Step 3: E2E du chrome en isolation**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release --filter "FullyQualifiedName~WindowChromeSmokeTests"`
Expected: PASS (ou skip propre).

- [ ] **Step 4: Smoke de rendu Photino final** (cf. Task 5 Step 5) — `received:true`, chromeless, contrôles OK.

- [ ] **Step 5: Vérif Linux (best-effort)** — si Docker dispo, lancer le DevHost/AppImage sous WebKitGTK + xvfb pour confirmer que le **repli** (chrome OS conservé) rend correctement ; sinon documenter la vérif AppImage manuelle. Le chromeless n'est PAS activé sous Linux (repli), donc pas de régression de drag.

- [ ] **Step 6: Push + PR**

```bash
git push -u origin feat/custom-window-chrome
gh pr create --base main --title "feat(chrome): fenêtre chromeless façon Discord (barre de titre intégrée)" --body "..."
```
PR FR : portée (chromeless + contrôles + drag, parité navigateur, repli Linux), fichiers, tests (counts), smoke de rendu. Terminer le corps par `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

---

## Release v4.0.0 (après merge du chrome — hors de ce plan d'implémentation)

Voir spec §7 : bump version → 4.0.0, `CHANGELOG [v4.0.0]` (QoL S1–S8 + chrome), main vert + dry-runs `release.yml` verts, puis **tag `v4.0.0` + push** → publication des installeurs. Vérifier le run `release.yml` et les artefacts.
