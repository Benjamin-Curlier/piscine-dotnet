# Retex — v4 S9b : câbler l'hôte Photino sur les services App (Router + pages + DI)

> Issue #42. Branche `v4/s9b-photino-wiring`. Plan : [../plans/2026-06-07-v4-s9b-photino-wiring.md](../plans/2026-06-07-v4-s9b-photino-wiring.md).
> **Verdict : objectif atteint.** L'app de bureau `Piscine.Desktop` n'est plus le spike S1 : elle monte
> le **Router** de la RCL + **toutes les pages du flux recrue** (cours/check/progress/init/resultat) +
> les **services `Piscine.App`**. Le risque dominant (render modes en hôte WebView) est levé par le motif
> **Microsoft Blazor Hybrid** (indirection, pas de globalisation). **Débloque S10 (#31, docs flux desktop).**
> Build 0 warning ; **247 tests verts** (DevHost E2E inclus) ; moteur/`Cli`/`release.yml` intacts ; aucun tag.

## Décisions techniques

- **Render mode host-agnostique = indirection `InteractiveRenderSettings`** (motif officiel MS « Blazor
  Hybrid + RCL partagée »). Les pages RCL **gardent** `@rendermode InteractiveServer`, mais via
  `@using static Piscine.Components.InteractiveRenderSettings` dans le `_Imports.razor` de la RCL, le
  symbole `InteractiveServer` résout désormais vers une **propriété statique** (= `RenderMode.InteractiveServer`
  par défaut). L'hôte Photino appelle `ConfigureBlazorHybridRenderModes()` au démarrage → les propriétés
  passent à **null** → `@rendermode null` = **rendu in-process** (les composants WebView sont interactifs
  par nature). DevHost (Web App) garde les valeurs par défaut → comportement **inchangé**.
- **POURQUOI PAS « render mode global côté DevHost ».** Première tentative (retirer `@rendermode` des
  pages + `<Routes @rendermode="InteractiveServer">` sur DevHost) : **rejetée**. L'E2E `ReaderSmokeTests`
  a **régressé** (1 KO) — rendre les pages **cours** (statiques SSR + nav enhanced) interactives casse le
  timing de **highlight.js** (les `<pre><code>` arrivent par rendu interactif *après* le `highlightAll()`
  du `load`). Le motif d'indirection laisse les pages cours **statiques** → 0 régression. **Leçon : la
  question gating n'était pas « enlever @rendermode » mais « Photino tolère-t-il @rendermode » ; réponse
  doc MS = non, mais on le neutralise par indirection, sans toucher au rendu statique du DevHost.**
- **DI Photino = port de `DevHost/Program.cs`** (singletons) MOINS le terminal : CourseCatalog,
  MarkdownRenderer, PiscineLayout (par env), GitStatusService, CheckService, ProgressService, InitService,
  `IPushResultWatcher`. App.razor monte un `<Router>` (`AdditionalAssemblies` = RCL, `DefaultLayout` =
  `MainLayout`, `NotFoundPage`). Un `_Imports.razor` neuf apporte les `@using` Blazor routing à l'hôte.
- **Terminal/coaching DÉFÉRÉ** (assumé) : dépend de `Piscine.GitShim` **non empaqueté** (hors release) +
  `@inject IHostEnvironment` (non fourni par Photino) + PTY. **NavMenu ne lie pas `/terminal`** et Photino
  n'a pas de barre d'URL → `TerminalPage` reste **inatteignable** (jamais instanciée → pas de crash DI).
  La recrue pousse via le terminal OS (`start-piscine-desktop` / `start-piscine`). Suivi.
- **Chemin du `content/` dans le zip = AUCUNE action.** `ContentRootResolver` **remonte** depuis
  `AppContext.BaseDirectory` jusqu'à un `content/modules`. Depuis `<zip>/desktop/Piscine.Desktop.exe`, il
  trouve `<zip>/content` (le frère, au parent) → l'app packagée trouve le contenu **sans** `PISCINE_CONTENT`
  ni modif de lanceur.

## Ce qui est PROUVÉ (par l'agent, automatique)

- **DevHost E2E (Playwright, vrai navigateur) : 8/8 verts** APRÈS l'indirection → les pages restent
  interactives serveur, coloration/sommaire/bascule thème OK (la régression de la 1ʳᵉ approche est levée).
- **Build solution 0 warning** ; **247 tests verts** (Core 46 + Components 24 + Git 7 + App 51 +
  Grading 111 + DevHost.E2E 8). +1 test : `InteractiveRenderSettingsTests` (défaut framework → null après
  `ConfigureBlazorHybridRenderModes`).
- **Smoke Photino (Windows)** : l'exe publié se lance, la fenêtre « Piscine .NET » s'ouvre, charge
  `http://localhost/` (hôte WebView), **reste vivante ~18 s, 0 crash** (DI résolue, CourseCatalog chargé,
  MainLayout/NavMenu/Home rendus sans exception au démarrage).
- **Gardes** : `git diff origin/main...HEAD -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git
  src/Piscine.Cli .github/workflows/release.yml` = **vide** ; **aucun tag**.

## Checklist smoke par OS — **À EXÉCUTER PAR LE PROPRIO** (la fenêtre native route)

Un agent ne peut pas piloter la fenêtre Photino (pas de barre d'URL, rendu natif). `dotnet build -c Release`
puis `dotnet run --project src/Piscine.Desktop -c Release` (ou le zip d'une pré-release) :

- [ ] **Windows** : la fenêtre s'ouvre sur l'**Accueil** ; le NavMenu liste les modules ; cliquer un
  module → cours (titre + gras + bloc de code **colorisé**) ; **Vérifier** (/check) sélectionne un exo et
  rend un verdict ; **Progression** (/progress) liste les statuts ; **Initialiser** (/init) ; **Résultat**
  (/resultat) s'affiche.
- [ ] **Linux** : `libwebkit2gtk-4.1` installé → mêmes écrans.
- [ ] **macOS** : mêmes écrans (WKWebView intégré).
- [ ] CLI **intact** dans le même zip (`piscine init`/`status`).

## Limites connues (acceptables, à suivre)

- **Terminal embarqué + coaching git absents de l'app packagée** (shim non empaqueté) → suivi dédié
  (packager `Piscine.GitShim` + fournir `IHostEnvironment`/garde adaptée + PTY in-process Photino). La
  page existe dans la RCL (DevHost), juste inatteignable dans Photino.
- **Rendu/navigation des pages Photino non vérifié par l'agent** (fenêtre native) → checklist proprio.
  Le smoke prouve le démarrage sans crash, pas le rendu visuel page par page.
- **`@rendermode null` en WebView non exercé en test automatique** : bUnit ignore les render modes ; la
  preuve runtime du chemin Photino reste le smoke proprio. La doc MS + le build + l'E2E DevHost (valeur
  par défaut) + le test d'indirection couvrent le reste.

## Pièges réutilisables (pour le HANDOFF / sprints suivants)

- **Partager une RCL entre Blazor Web App (DevHost) et BlazorWebView (Photino)** = motif MS
  `InteractiveRenderSettings` : pages avec `@rendermode InteractiveServer`, symbole résolu par
  `@using static <RCL>.InteractiveRenderSettings`, l'hôte WebView appelle `ConfigureBlazorHybridRenderModes()`
  (→ null). **NE PAS** globaliser le render mode côté Web App (casse le rendu statique des pages contenu /
  le timing JS post-`load`). **Vérifier l'hypothèse gating via la doc** avant de refactorer (ici : doc MS).
- **Un hôte Photino a besoin de son propre `_Imports.razor`** (routing/web `@using`) : sans lui, `Router`/
  `Found`/`FocusOnNavigate` ne résolvent pas (RZ10012). DevHost l'avait ; le spike Desktop ne l'avait pas.
- **`Router` dans une RCL consommée par Photino** : `App.razor` Photino = `<Router AppAssembly=...
  AdditionalAssemblies="new[]{ typeof(<TypeRCL>).Assembly }" NotFoundPage="typeof(<PageRCL>)">`. Utiliser
  le **paramètre `NotFoundPage`** plutôt que le template `<NotFound>` évite la collision de nom avec une
  page nommée `NotFound`.
- **`ContentRootResolver` remonte depuis `AppContext.BaseDirectory`** → un exe dans un sous-dossier
  (`desktop/`) trouve un `content/` frère au parent **sans** `PISCINE_CONTENT`. Utile pour des layouts de
  zip à sous-dossiers.
- **Smoke d'un GUI `WinExe`** : `timeout <N> <exe>` ; **EXIT=124** (tué par timeout) = « resté vivant N s »
  = succès du smoke « se lance / 0 crash ». Les exceptions de *rendu* (post-démarrage) ne remontent PAS sur
  stdout (WebView) → restent vérif proprio.
