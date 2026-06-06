# v4 Sprint 6 — Lecteur de cours/sujets (RCL) + parité mode sombre — Plan d'implémentation

> Issue #27 (milestone « v4 — application desktop Photino »). Branche : `v4/s6-course-reader`.
> Spec : [2026-06-06-v4-photino-desktop-design.md](../specs/2026-06-06-v4-photino-desktop-design.md) §2
> (« lecture de cours »), §4 (RCL partagée par les deux hôtes). Plans précédents :
> [S1](2026-06-06-v4-s1-foundation.md) (RCL, `MarkdownView`/`MarkdownRenderer`, pages cours/module/exo),
> [S3](2026-06-06-v4-s3-git-coaching.md) (précédent xterm.css : asset RCL `wwwroot` servi sous `_content/`),
> [S5](2026-06-06-v4-s5-navigation-progression.md). **Dépend de S1.**

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development ou
> superpowers:executing-plans, tâche par tâche. Steps en cases `- [ ]`. Commits conventionnels FR
> (le parent ajoute le trailer `Co-Authored-By`). **Aucune nouvelle dépendance NuGet** : on réutilise
> `Piscine.Core.Content.CourseAnchors` (moteur, NON modifié), Markdig 0.41.3 (RCL S1), bUnit 2.7.2
> (`BunitContext`/`Render<T>`), Playwright (S1). **Moteur (`Core`/`Grading`/`Git`) + `Piscine.Cli` +
> `release.yml` INTACTS** : `CourseAnchors` est LU, jamais modifié.

## Délta (recherche faite le 2026-06-06) — ce que S1 a DÉJÀ livré vs ce qui est NOUVEAU en S6

**Déjà fait (S1, NE PAS replanifier)** — dans `src/Piscine.Components` :
- Rendu Markdig complet : `MarkdownView.razor` + `Services/MarkdownRenderer.cs` (pipeline
  `UseAdvancedExtensions` + `UseAutoIdentifiers(GitHub)` + `UseSoftlineBreakAsHardlineBreak`).
- Pages du lecteur : `Components/Pages/{Home,Module,Exercise}.razor` (cours, sujets, fil d'Ariane,
  cartes d'exos, pagers). Layout : `Components/Layout/{MainLayout,NavMenu}.razor` — le bouton
  `theme-toggle` **existe déjà** dans `MainLayout` et appelle `toggleTheme()`.
- Coloration code : highlight.js câblé **host-level** dans `Piscine.DevHost/Components/App.razor`.
- Le DevHost rend déjà un cours **à parité avec l'ancien site, mode sombre compris**.

**NOUVEAU (S6, le delta réel)** :
1. **Sommaire (table des matières)** — n'existe nulle part. Composant RCL `CourseToc.razor` réutilisant
   `CourseAnchors.Extract` → ancres `#slug`. C'est le « meilleur que l'ancien site ». Câblé dans
   `Module.razor` (cours) et `Exercise.razor` (sujet).
2. **Parité mode sombre Photino** — le thème (CSS `[data-theme]` + toggle JS + highlight.js) vit
   **uniquement** dans `Piscine.DevHost` ; l'hôte Photino `Piscine.Desktop` n'a **aucun** CSS ni thème
   (son `index.html` statique ne charge rien et ne monte que `MarkdownView` en dur). S6 rend le thème
   **partageable** (RCL) pour que les DEUX hôtes l'aient, DevHost inchangé-ou-meilleur.

**Hors S6 (suivi, ne pas faire ici)** : câbler le Router/`CourseCatalog` complet dans l'hôte Photino
(suivi HANDOFF : aujourd'hui spike qui ne monte que `MarkdownView`) — S6 pose les **assets** de parité
(CSS/JS/coloration) ; le routage complet Photino est un sprint dédié. S6 prouve la parité thème par le
DevHost (Playwright) + un smoke « Photino se lance, 0 exception, charge le CSS ».

**Approche parité mode sombre décidée** : déplacer le CSS thème global vers
`src/Piscine.Components/wwwroot/css/piscine.css` (verbatim) et le toggle JS vers
`src/Piscine.Components/wwwroot/js/theme.js`, servis aux deux hôtes sous `_content/Piscine.Components/...`
(précédent xterm.css S3). DevHost : `@Assets["_content/Piscine.Components/css/piscine.css"]` (fingerprinté) ;
Photino : lien `_content/...` **nu** dans `index.html` (Photino ne fait pas tourner `@Assets`/`ImportMap`).
Coloration highlight.js **reste host-level** (CDN + `highlightAll` sur enhanced-load) ; on l'ajoute juste à
l'hôte Photino. `__applyTheme()` reste appelé inline (avant 1er paint, anti-flash). DevHost **inchangé-ou-meilleur**.

**Goal:** Compléter le lecteur pour atteindre la parité (et un cran au-dessus) avec l'ancien `Piscine.Web` :
(1) un **sommaire** réutilisable généré depuis les titres du cours, et (2) la **parité mode sombre** dans les
DEUX hôtes — en extrayant le thème hors du seul `Piscine.DevHost` vers la RCL. Le rendu Markdig, la
coloration et les pages **existent déjà depuis S1** : ce sprint scope le **delta**, sans réécrire l'existant.

**Architecture:** Tout vit dans la RCL `Piscine.Components` (consommée à l'identique par `Piscine.DevHost`
**et** `Piscine.Desktop` — spec §4). Le **sommaire** = composant pur `CourseToc.razor` qui appelle
`CourseAnchors.Extract` (moteur, lecture seule) → ancres `#slug` cohérentes avec Markdig
(`UseAutoIdentifiers(GitHub)`). Le **thème** migre vers `Piscine.Components/wwwroot/{css/piscine.css, js/theme.js}`,
servi sous `_content/Piscine.Components/...`. La coloration highlight.js reste host-level. DevHost
**identique-ou-meilleur** (critère d'acceptation).

**Tech Stack:** .NET 10 ; Blazor (RCL + Server + Photino WebView) ; Markdig 0.41.3 (S1) ;
`Piscine.Core.Content.CourseAnchors` (moteur, LU) ; xUnit ; bUnit 2.7.2 (`BunitContext`/`Render<T>`) ;
Playwright (skip-sans-Chromium, racine via `Piscine.slnx`, parallélisation désactivée, port dédié).
**Aucune nouvelle référence NuGet.**

---

## ⚠️ Note de risque

**Risque principal** — déplacer le thème *host-level* du seul `Piscine.DevHost` vers la RCL **sans casser
le mode sombre qui marche aujourd'hui** + le **piège des clés `_content/`** (S1 : clé `@Assets` nue → 404
sur `ReconnectModal.razor.js` ; leçon `Piscine.DevHost.styles.css`). Le CSS thème global (`:root`/`[data-theme]`)
**n'est PAS scopé** → il ne se bundle pas seul comme un `.razor.css` : le servir depuis `wwwroot` sous
`_content/Piscine.Components/...` (précédent xterm.css S3). DevHost le référence via
`@Assets["_content/Piscine.Components/css/piscine.css"]` (fingerprinté) ; Photino par un lien `_content/` nu
dans `index.html` statique. Mitigation : réutiliser le motif xterm.css, **changer un `<link>` à la fois**,
vérifier par Playwright que `data-theme` bascule **et** que le code reste colorisé, avant/après.

**Risque secondaire** — les ancres du sommaire doivent matcher **les ids réellement émis par Markdig**.
`CourseAnchors.Extract` reflète `UseAutoIdentifiers(GitHub)`, **mais** renvoie un `IReadOnlySet<string>` qui
ne modélise pas les suffixes `-1`/`-2` que Markdig ajoute aux titres en double. Mitigation : générer le
sommaire **dans l'ordre du document** (titres parcourus séquentiellement), + un test bUnit qui asserte
qu'un `<a href="#slug">` du sommaire pointe vers un `id` réellement présent dans le HTML rendu par
`MarkdownRenderer`. Les cours du curriculum n'ont pas de titres dupliqués → cas nominal sûr ; le test verrouille.

## Carte des fichiers

- Créer : `src/Piscine.Components/Components/CourseToc.razor` (+ `.razor.css`) — sommaire pur, `CourseAnchors.Extract`
- Créer : `src/Piscine.Components/wwwroot/css/piscine.css` — thème global (déplacé verbatim depuis `Piscine.DevHost/wwwroot/app.css`)
- Créer : `src/Piscine.Components/wwwroot/js/theme.js` — `__applyTheme`/`toggleTheme`/`updateThemeIcon` (déplacés depuis `App.razor`)
- Modifier : `src/Piscine.Components/Components/Pages/Module.razor` + `Exercise.razor` (insérer `<CourseToc Markdown="..." />`)
- Modifier : `src/Piscine.DevHost/Components/App.razor` (référencer le CSS/JS RCL ; retirer le toggle JS inline déplacé ; GARDER l'appel `__applyTheme()` anti-flash + highlight.js host)
- Modifier/supprimer : `src/Piscine.DevHost/wwwroot/app.css` (remplacé par le `<link>` `_content/...` ; ne PAS toucher `Piscine.DevHost.styles.css`)
- Modifier : `src/Piscine.Desktop/wwwroot/index.html` (liens `_content/Piscine.Components/css/piscine.css` + `js/theme.js` ; highlight.js CDN + `highlightAll` inline)
- Test : `tests/Piscine.Components.Tests/CourseTocTests.cs` (bUnit) ; `tests/Piscine.DevHost.E2E/ReaderSmokeTests.cs` (Playwright)
- **NE PAS toucher** : `src/Piscine.Core` (dont `CourseAnchors.cs` — LU), `src/Piscine.Grading`, `src/Piscine.Git`, `src/Piscine.Cli`, `.github/workflows/release.yml`.

---

### Task 1 : composant `CourseToc` (sommaire) réutilisant `CourseAnchors.Extract` + bUnit

**Files:**
- Create: `src/Piscine.Components/Components/CourseToc.razor` (+ `CourseToc.razor.css`)
- Test: `tests/Piscine.Components.Tests/CourseTocTests.cs`

- [ ] **Step 0 — Vérifier l'API moteur LUE** : relire `src/Piscine.Core/Content/CourseAnchors.cs`.
  Confirmer `public static IReadOnlySet<string> Extract(string courseMarkdown)` et le slugify GitHub
  (minuscules, ponctuation supprimée, séparateurs → un tiret, `{#id}` explicite prioritaire). **NE PAS
  MODIFIER ce fichier** (moteur, gate `validate-content`).

- [ ] **Step 1 — Le composant** `CourseToc.razor`. Pur, sans état, sans DI. Parcourt le markdown
  **ligne par ligne dans l'ordre du document** (règles ATX : 1–6 `#` + espace) → liste ordonnée
  `(int Level, string Text, string Slug)`. Détails :
  - `[Parameter] public string? Markdown { get; set; }` + `[Parameter] public int MinLevel { get; set; } = 2;`
    (par défaut `##`/`###` ; le `#` titre principal n'est pas une entrée).
  - Rendu : `<nav class="toc" data-testid="course-toc">` → `<ul>` (indenté par niveau), chaque entrée
    `<a href="#@slug">@text</a>`. Si aucune entrée ≥ `MinLevel`, **ne rien rendre** (pas de `<nav>` vide).
  - **Cohérence d'ancre** : répliquer minimalement la dérivation de slug de `CourseAnchors` (titre nettoyé →
    `{#id}` explicite sinon slug GitHub) ; `CourseAnchors.Slugify` est `private`, donc on réplique DANS le
    composant ET on asserte au test (T1) que chaque slug ∈ `CourseAnchors.Extract(markdown)` (garde-fou
    anti-dérive). **Par défaut on NE touche pas le moteur** (pas d'exposition d'un helper public).

- [ ] **Step 2 — `CourseToc.razor.css`** (scopé, auto-bundlé — **aucune clé `@Assets`**). Style sobre :
  `.toc` en encart, liens `var(--ink-soft)` → `var(--accent)` au survol, indentation par niveau ; utiliser
  les variables CSS du thème (Task 2) → hérite du mode sombre automatiquement.

- [ ] **Step 3 — bUnit** `CourseTocTests.cs` (`BunitContext`, `Render<T>()`). Cas :
  - **Un lien par titre** : `"# Cours\n\n## Partie A\n\n### Détail\n\n## Partie B"` →
    `[data-testid="course-toc"]` présent, 3 `<a>` (les `##`/`###`, pas le `#`), textes « Partie A », « Détail », « Partie B ».
  - **Ancres cohérentes Markdig** : pour chaque `<a href="#X">`, asserter `X ∈ CourseAnchors.Extract(markdown)`
    ET, robustesse, rendre le **même** markdown via `MarkdownRenderer` (enregistré en DI comme `MarkdownViewTests`)
    et asserter que le HTML contient `id="X"`.
  - **Vide → rien** : markdown sans titre ≥ `MinLevel` (ou `null`) → aucun `[data-testid="course-toc"]`.

- [ ] **Step 4 — Build + test**

Run: `dotnet test tests/Piscine.Components.Tests/Piscine.Components.Tests.csproj -c Release`
Expected: build **0 warning** ; `CourseTocTests` PASS ; `MarkdownViewTests`/`CheckFeedbackTests`/`StatusBadgeTests` verts.

- [ ] **Step 5 — Commit**

```bash
git add -A
git commit -m "feat(v4): composant CourseToc (sommaire genere depuis les titres, ancres coherentes Markdig via CourseAnchors) + bUnit"
```

### Task 2 : thème (CSS variables + toggle JS) déplacé en asset RCL partagé — DevHost inchangé-ou-meilleur

**Files:**
- Create: `src/Piscine.Components/wwwroot/css/piscine.css`, `src/Piscine.Components/wwwroot/js/theme.js`
- Modify: `src/Piscine.DevHost/Components/App.razor`, `src/Piscine.DevHost/wwwroot/app.css`

> Précédent éprouvé (S3) : `Piscine.Components/wwwroot/lib/xterm/xterm.css` est servi sous
> `_content/Piscine.Components/lib/xterm/xterm.css` et référencé via `@Assets["_content/Piscine.Components/lib/xterm/xterm.css"]`.
> Même motif pour le CSS thème et le JS thème. Piège S1 : la clé `@Assets` doit être **préfixée**
> `_content/Piscine.Components/...` (clé nue → 404). Le `.razor.css` se bundle seul ; le CSS **global**
> (`:root`/`[data-theme]`) **non** → on le sert depuis `wwwroot`.

- [ ] **Step 1 — Déplacer le CSS thème dans la RCL** : copier **verbatim** `src/Piscine.DevHost/wwwroot/app.css`
  (variables `:root`/`:root[data-theme="dark"]`, layout, sidebar, markdown, badges, notices, pager, responsive,
  `#blazor-error-ui`) vers `src/Piscine.Components/wwwroot/css/piscine.css`. **Aucune** modification de règle
  (parité = critère). Commentaire en tête « thème partagé RCL (DevHost + Photino) ».

- [ ] **Step 2 — Déplacer le toggle JS dans la RCL** : `src/Piscine.Components/wwwroot/js/theme.js` exposant
  **les mêmes** globales que `App.razor` aujourd'hui : `window.__applyTheme`, `window.toggleTheme`,
  `window.updateThemeIcon` (copie verbatim de la logique `localStorage`/`data-theme`/icône `☾`/`☀`). **Ne pas**
  y mettre `highlightAll`/`onPageReady` (reste host-level). `MainLayout` appelle déjà `toggleTheme()` → inchangé.

- [ ] **Step 3 — DevHost référence les assets RCL** dans `App.razor` :
  - Remplacer `<link ... "@Assets["app.css"]" />` par `<link rel="stylesheet" href="@Assets["_content/Piscine.Components/css/piscine.css"]" />`.
  - **Garder** `Piscine.DevHost.styles.css` (bundle scopé — leçon S1), `_content/Piscine.Components/lib/xterm/xterm.css`, le `<link>` github-dark highlight.js.
  - **Garder l'appel anti-flash** `window.__applyTheme()` dans `<head>`, mais charger la **définition** depuis la RCL
    juste avant : `<script src="@Assets["_content/Piscine.Components/js/theme.js"]"></script>` avant le `<script>` inline qui
    appelle `__applyTheme()`. (Ou garder une fonction `__applyTheme` inline minimale pour l'anti-flash + charger theme.js
    pour `toggleTheme`/`updateThemeIcon` — choisir la forme qui garde l'anti-flash garanti ; commenter.)
  - Dans le `<script>` host inline : **retirer** `toggleTheme`/`updateThemeIcon`/`__applyTheme` (désormais dans theme.js) ;
    **garder** `highlightAll()` + `onPageReady()` + les abonnements `DOMContentLoaded`/`load`/`enhancedload`.

- [ ] **Step 4 — Vider/rediriger `app.css`** : `src/Piscine.DevHost/wwwroot/app.css` n'est plus la source du thème.
  Le supprimer (et de toute référence — fait Step 3), OU le réduire à un fichier vide commenté. Préférer la
  suppression propre si la ligne `@Assets["app.css"]` a bien été remplacée. **Ne pas** toucher `Piscine.DevHost.styles.css`.

- [ ] **Step 5 — Vérif visuelle DevHost (preview)** : `dotnet run --project src/Piscine.DevHost --urls http://localhost:5256`
  (avec `PISCINE_CONTENT`), `preview_start` sur une page module → cours stylé **identique**, bouton thème bascule
  clair/sombre (`data-theme` sur `<html>`), blocs de code colorisés, sommaire stylé. `preview_screenshot` clair + sombre.

- [ ] **Step 6 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: **0 warning** ; le DevHost démarre ; le CSS thème vient de `_content/Piscine.Components/css/piscine.css`.

- [ ] **Step 7 — Commit**

```bash
git add -A
git commit -m "refactor(v4): theme (CSS variables + toggle JS) deplace en asset RCL partage (_content/Piscine.Components) ; DevHost reference la RCL, rendu inchange"
```

### Task 3 : câbler le sommaire dans les pages + parité thème dans l'hôte Photino

**Files:**
- Modify: `src/Piscine.Components/Components/Pages/Module.razor`, `Exercise.razor`
- Modify: `src/Piscine.Desktop/wwwroot/index.html`

- [ ] **Step 1 — Sommaire page module** `Module.razor` : insérer `<CourseToc Markdown="@_module.CourseMarkdown" />`
  **avant** le `<MarkdownView Markdown="@_module.CourseMarkdown" />` (sous le fil d'Ariane). (Vérifier le nom réel
  de la propriété du markdown de cours sur le modèle.) Ne rend rien si le cours n'a pas de titre ≥ niveau 2.

- [ ] **Step 2 — Sommaire page exercice** `Exercise.razor` : insérer `<CourseToc Markdown="@_exercise.SubjectMarkdown" />`
  avant le `<MarkdownView ... />` du sujet (se masque tout seul si peu de titres).

- [ ] **Step 3 — Parité thème hôte Photino** `src/Piscine.Desktop/wwwroot/index.html`. Photino sert les assets
  statiques sous `_content/...` mais **ne fait pas** `@Assets`/`ImportMap` → **liens nus**. Dans `<head>` :
  - `<script>window.__applyTheme=function(){var t=localStorage.getItem('theme')||(window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light');document.documentElement.setAttribute('data-theme',t);};window.__applyTheme();</script>` (anti-flash).
  - `<link rel="stylesheet" href="_content/Piscine.Components/css/piscine.css" />`
  - `<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.10.0/styles/github-dark.min.css" />`
  - Avant `</body>` : `<script src="_content/Piscine.Components/js/theme.js"></script>`,
    `<script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.10.0/highlight.min.js"></script>`, puis un
    `<script>` inline `function highlightAll(){...}` + `window.addEventListener('load',function(){window.__applyTheme&&window.__applyTheme();window.updateThemeIcon&&window.updateThemeIcon();highlightAll();});`. **Garder**
    `<script src="_framework/blazor.webview.js"></script>`.
  - **Note** : S6 ne câble PAS le Router complet de la RCL dans Photino (suivi) ; `App.razor` Photino monte
    toujours `MarkdownView` (spike). Objectif S6 ici = **les assets de parité chargent dans la webview**, validé au smoke.

- [ ] **Step 4 — Smoke Photino (agent partiel + proprio)** : `dotnet run --project src/Piscine.Desktop -c Release`.
  Expected (agent, logs) : fenêtre lancée, vivante ~12 s, **0 exception**. Agent ne voit pas la fenêtre →
  **vérif visuelle proprio** : `MarkdownView` désormais **stylé** (police, code colorisé, fond clair/sombre selon
  le système). Noter dans le retex. (Si `_content/...` ne se sert pas dans la webview Photino, replier : copier les
  assets dans `Piscine.Desktop/wwwroot/` + liens locaux — documenter.)

- [ ] **Step 5 — Build**

Run: `dotnet build Piscine.slnx -c Release`
Expected: **0 warning** ; `CourseToc` rendu dans `/module` et `/module/{id}/{exo}` ; `index.html` Photino référence le thème RCL.

- [ ] **Step 6 — Commit**

```bash
git add -A
git commit -m "feat(v4): sommaire (CourseToc) cable dans pages module/exercice + parite theme/coloration dans l'hote Photino (index.html -> _content/Piscine.Components)"
```

### Task 4 : E2E Playwright (cours + sommaire + code colorisé + bascule clair/sombre)

**Files:**
- Create: `tests/Piscine.DevHost.E2E/ReaderSmokeTests.cs`

- [ ] **Step 1 — Le test** : squelette `SmokeTests`/`ProgressSmokeTests` (port dédié **5257**, poll, **skip propre
  sans Chromium**, racine via `Piscine.slnx`, kill arbre en `DisposeAsync`, `PISCINE_CONTENT`). Sur une page module
  riche en titres :
  - **Cours + sommaire** : `GotoAsync("/module/<id>")`, attendre `h1`, asserter `[data-testid="course-toc"]` présent
    + au moins un `nav.toc a[href^="#"]`.
  - **Ancre fonctionnelle** : cliquer le 1er lien du sommaire → l'URL contient `#slug` et l'élément `id="slug"` existe.
  - **Code colorisé** : au moins un `pre code` porte la classe `hljs` (ou `span.hljs-*`) après chargement.
  - **Bascule clair/sombre** : lire `data-theme`, cliquer `#theme-toggle`, asserter que `data-theme` a changé (light↔dark)
    et persiste (`localStorage.theme`).

- [ ] **Step 2 — Exécuter**

Run: `dotnet test tests/Piscine.DevHost.E2E -c Release`
Expected: PASS (skip propre sans Chromium en CI).

- [ ] **Step 3 — Commit**

```bash
git add -A
git commit -m "test(v4): E2E lecteur (cours + sommaire + ancres + code colorise + bascule data-theme clair/sombre)"
```

### Task 5 : vérification globale + garde-fous (moteur intact) + PR

- [ ] **Step 1 — Build + tests solution**

Run: `dotnet build Piscine.slnx -c Release` puis `dotnet test Piscine.slnx -c Release`
Expected: build **0 warning** ; tous verts (219 S5 + `CourseTocTests` (~3) ; E2E se sautent proprement sans Chromium).

- [ ] **Step 2 — Garde-fous (moteur intact)** :

```bash
git diff --name-only origin/main -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli .github/workflows/release.yml
```

Expected: **aucune** sortie. `CourseAnchors` est **LU** (`Extract`), jamais modifié.

- [ ] **Step 3 — Contenu non régressé**

Run: `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content`
Expected: « Contenu valide. »

- [ ] **Step 4 — Garde-fou parité DevHost** : confirmer (preview T2 Step5 + E2E T4) que le DevHost rend le cours
  **identique-ou-meilleur** (même style + sommaire), thème + coloration OK. `Piscine.DevHost.styles.css` **non touché**.

- [ ] **Step 5 — PR** (commit et push en **appels séparés**)

```bash
git push -u origin v4/s6-course-reader
gh pr create --base main --title "v4 S6 — lecteur de cours/sujets (sommaire) + parite mode sombre (theme partage RCL)" --body-file <fichier>
```

---

## Self-review (couverture S6 vs issue #27 / spec §2,§4)

- **Objectif** « lecteur complet à parité (mode sombre inclus) » → rendu Markdig/pages **existait (S1)** ; S6 ajoute
  le **delta** : sommaire (T1+T3) + **parité mode sombre dans les deux hôtes** via thème partagé RCL (T2+T3). ✅
- **Périmètre** : rendu Markdig (réutilisé S1) ✅ · coloration (host-level, étendue à Photino T3) ✅ · **sommaire**
  (`CourseToc` T1, câblé T3) ✅ · **mode sombre** (CSS `[data-theme]` + toggle migrés en RCL T2 ; appliqués à Photino T3) ✅.
- **Critères d'acceptation** : cours identique/meilleur (parité S1 + sommaire ; garde-fou T2 Step5 / T5 Step4) ✅ ·
  bascule clair/sombre (toggle RCL, E2E `data-theme` T4) ✅ · **bUnit** (`CourseTocTests` T1) ✅ · **E2E** (T4) ✅.
- **Dépendances** : **S1** (RCL, `MarkdownView`/`MarkdownRenderer`, pages, layout/toggle) ✅.
- **Pièges v4 réutilisés** : WarningsAsErrors **0 warning** · moteur + `Cli` + `release.yml` **INTACTS**, `CourseAnchors`
  LU non modifié (gate diff vide T5) · **clés `_content/Piscine.Components/...`** pour CSS/JS thème (précédent xterm.css S3 ;
  piège clé nue → 404 S1) · **CSS global non scopé servi depuis `wwwroot`** (≠ `.razor.css` auto-bundlé ; `CourseToc.razor.css`
  scopé, aucune clé) · `Piscine.DevHost.styles.css` **non touché** (leçon S1) · pages `Module`/`Exercise` en SSR statique (le
  sommaire est du markup ; le toggle est du JS pur, pas un handler Blazor) · bUnit 2.x (`MarkdownRenderer` en DI) · Playwright
  skip-sans-Chromium + racine `Piscine.slnx` + parallélisation désactivée + port 5257 · hôte Photino = `index.html` statique
  sans `@Assets` → **liens `_content/` nus**.
- **Risque principal maîtrisé** : thème migré sans casser le DevHost (motif xterm.css, un `<link>` à la fois, preview
  clair/sombre T2 + E2E `data-theme` T4 avant PR). **Secondaire** : ancres sommaire ↔ ids Markdig verrouillées par test
  (slug ∈ `CourseAnchors.Extract` ET `id="slug"` dans le HTML, T1) ; sommaire généré **dans l'ordre du document**.
- **Pas de gold-plating** : rendu/pages PAS réécrits ; routage complet Photino reste un **suivi** — S6 ne livre que les
  **assets de parité** côté Photino, validés au smoke.
