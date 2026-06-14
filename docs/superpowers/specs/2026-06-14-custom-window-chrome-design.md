# Spec — Chrome de fenêtre personnalisé (façon Discord) + release v4.0.0

> Brainstorming validé le 2026-06-14. Retire le chrome OS par défaut de l'app de bureau Photino et
> intègre les contrôles de fenêtre dans la barre de navigation existante (barre unique, draggable), puis
> publie **v4.0.0** (qui cumule l'épic QoL desktop S1–S8 déjà mergé + ce chrome custom).

## 1. Contexte

- App de bureau **Photino** (`src/Piscine.Desktop`) via **PhotinoX.Blazor 4.2.0** (fork net10-natif ;
  namespace `Photino.Blazor` conservé). Fenêtre configurée dans `src/Piscine.Desktop/Program.cs`
  (`app.MainWindow.SetTitle("Piscine .NET").SetUseOsDefaultSize(true)`), juste avant `app.Run()`.
- La fenêtre actuelle affiche **2 barres empilées** : le **chrome OS** (titre « Piscine .NET » +
  boutons réduire/agrandir/fermer du système) AU-DESSUS de la **navbar applicative** (`MainLayout.razor` :
  marque `{ } Piscine .NET` + `NavTabs` + lien GitHub + bouton thème). On veut **supprimer la barre OS** et
  obtenir **une seule barre intégrée** (référence : Discord).
- Le même `MainLayout`/RCL `Piscine.Components` sert **deux hôtes** : `Piscine.Desktop` (fenêtre Photino
  réelle) et `Piscine.DevHost` (site Blazor dans un **navigateur**, dev + tests bUnit/Playwright). Le
  DevHost **n'a pas de fenêtre native** → les contrôles de fenêtre n'y ont aucun sens.
- Pont JS↔hôte déjà en place : `index.html` détecte Photino via `window.external && window.external.sendMessage`
  (utilisé par `SmokeProbe`). On réutilise ce canal.

## 2. Objectifs / Non-objectifs

**Objectifs**
- Fenêtre **chromeless** (sans chrome OS) sur l'hôte Photino.
- **Barre unique** : la navbar existante devient la barre de titre — zone vide draggable, double-clic =
  bascule agrandir/restaurer, contrôles **réduire / agrandir-restaurer / fermer** à l'extrême droite.
- **Parité** : dans le navigateur (DevHost), contrôles de fenêtre **masqués** et drag **inerte** ; la
  navbar et la navigation restent intactes.
- **Cross-plateforme** : fonctionne sous Windows (WebView2) **et** Linux (WebKitGTK / AppImage), avec un
  **repli gracieux vers le chrome OS sur Linux** si le drag WebKitGTK s'avère non fiable.
- Publier **v4.0.0** (chrome custom + épic QoL desktop S1–S8).

**Non-objectifs (YAGNI)**
- Pas de menus natifs custom, pas de tray icon, pas de multi-fenêtres.
- Pas de snap « moitié d'écran » au bord (le package CustomWindow le fait ; hors périmètre — on garde
  seulement maximiser/restaurer).
- macOS : déjà abandonné dans le projet (cf. [[piscine-dotnet-project]]) — non concerné.
- Moteur / `Piscine.Cli` / `grade-received` / `release.yml` (logique) : **gelés**.

## 3. Approche retenue

**A — Couche chromeless maison, sans package tiers.** Activer `Chromeless` sur la fenêtre Photino,
transformer la navbar en barre de titre draggable, ajouter les contrôles de fenêtre, et câbler drag +
contrôles via le pont message JS↔hôte déjà utilisé. Aucune nouvelle dépendance ; compatible avec le fork
PhotinoX ; `MainLayout` conservé.

Rejeté : **B** `Photino.Blazor.CustomWindow` (dépend de l'upstream `Photino.Blazor`, conflit probable avec
le fork PhotinoX + compat net10 inconnue, veut posséder le layout) ; **C** recolorer le chrome OS (ne
supprime pas le rendu par défaut).

## 4. Architecture & composants

### 4.1 Hôte Photino (`Piscine.Desktop`)
- **Activer le chromeless** : `app.MainWindow.SetChromeless(true)` **avant `app.Run()`** (la fenêtre native
  n'est instanciée qu'au `Run()` → réglage pré-Run OK, comme `SetTitle`/`SetUseOsDefaultSize` actuels).
  Garder `Resizable`. ⚠️ Vérifier à l'implémentation : sous chromeless, l'OS retire-t-il la bordure de
  redimensionnement ? (sinon prévoir des poignées CSS, cf. §5.3).
- **Gestionnaire de messages fenêtre** : un service hôte (`WindowChromeHost`) enregistré via
  `RegisterWebMessageReceivedHandler` qui interprète les messages `PISCINE_WIN:<action>` et pilote la
  fenêtre Photino :
  - `minimize` → `MainWindow.SetMinimized(true)`
  - `maximize` / `restore` → `SetMaximized(true/false)` (suivre l'état pour basculer)
  - `close` → `MainWindow.Close()`
  - `drag:<dx>,<dy>` (ou flux de positions) → `SetLeft/SetTop` (déplacement piloté JS — technique éprouvée
    de CustomWindow ; pas d'API « begin drag » native dans Photino).
  - À l'inverse, l'hôte **notifie** la page de l'état agrandi (message `PISCINE_WIN_STATE:maximized|normal`)
    pour le style (§5.4). Coexiste avec `SmokeProbe` (préfixes de message distincts).
  - ⚠️ Noms exacts d'API PhotinoX/Photino.NET à confirmer à l'implémentation (`Chromeless`, `Minimized`,
    `Maximized`, `Left`/`Top`/`Location`, `Close`). Le plan inclura une étape de vérification de surface d'API.

### 4.2 RCL `Piscine.Components` (UI, agnostique de l'hôte)
- **`WindowControls.razor`** (+ `.razor.css`) : cluster de 3 boutons (réduire `–`, agrandir/restaurer
  `⤢`/`⧉`, fermer `✕`) rendu à l'extrême droite de la navbar, **après** le bouton thème. Chaque bouton =
  `no-drag` ; au clic, appelle le module JS pour envoyer `PISCINE_WIN:<action>`. **Masqué quand l'hôte
  n'est pas Photino** (détection JS, cf. 4.3). Accessible (aria-label FR, `:focus-visible` hérité de S7).
- **`MainLayout.razor`** : monter `<WindowControls />` dans `.navbar` (à droite, dans/après `.navbar-links`).
  Marquer la `.navbar` comme **zone de drag** ; marquer les éléments interactifs (liens nav, GitHub, thème,
  contrôles) `no-drag`. Édition additive, ne pas casser skip-link/ARIA (S7).
- **`wwwroot/js/windowChrome.js`** (RCL) : (a) détecte l'hôte Photino (`window.external?.sendMessage`) et
  pose un flag (`document.documentElement.dataset.host = 'photino'|'browser'`) + `window.__isPhotino`;
  (b) attache le drag (pointerdown sur zone drag → suit le pointeur → envoie le déplacement à l'hôte ;
  double-clic → `PISCINE_WIN:maximize|restore`) ; (c) expose `winControl(action)` pour les boutons ;
  (d) écoute `PISCINE_WIN_STATE` pour basculer une classe `is-maximized` sur `<html>`. **Inerte en
  navigateur** (pas d'`external`). Référencé par les `index.html`/`App.razor` des deux hôtes (no-op en browser).
- **`wwwroot/css/piscine.css`** : styles barre de titre (zone drag : `app-region`/`-webkit-app-region: drag`
  en complément du JS là où supporté ; curseur ; hauteur), styles des 3 contrôles (hover, focus, état
  fermer en rouge), coins arrondis + ombre de la fenêtre quand **non** maximisée, et **suppression** des
  coins arrondis/ombre quand `html.is-maximized`. Respecter thème clair/sombre (S6) + a11y AA (S7).

### 4.3 Détection d'hôte & parité
- Source de vérité : présence de `window.external.sendMessage` (Photino) — déjà le signal du smoke.
- En **navigateur** : `WindowControls` masqué (CSS `html:not([data-host="photino"]) .window-controls{display:none}`
  + garde JS), drag inerte → DevHost / Playwright inchangés.
- Aucune logique fenêtre dans la RCL côté C# : la RCL ne fait qu'émettre des messages ; **l'hôte Desktop**
  les exécute. DevHost n'enregistre aucun handler fenêtre (no-op naturel).

## 5. Comportements détaillés

### 5.1 Contrôles
- Réduire → fenêtre minimisée. Agrandir ↔ Restaurer (le bouton et l'icône basculent selon l'état).
  Fermer → ferme l'app.
### 5.2 Déplacement (drag)
- pointerdown sur la zone draggable de la navbar (hors éléments `no-drag`) → déplacement de la fenêtre
  suivant le pointeur (flux de positions vers l'hôte). Double-clic → bascule agrandir/restaurer.
- ⚠️ **Risque principal** : fluidité du déplacement piloté JS et différences WebView2 vs WebKitGTK.
  Mitigation : sur Windows, tester aussi `app-region: drag` natif (WebView2 récents le supportent) et le
  préférer s'il fonctionne ; sinon flux JS. **Sur Linux**, si le drag WebKitGTK est saccadé/instable →
  **repli : ne PAS activer chromeless sous Linux** (garder le chrome OS), via un interrupteur à
  l'exécution (ex. `OperatingSystem.IsLinux()` ou variable d'env). La barre de titre custom s'affiche
  alors sans les contrôles (l'OS les fournit) — la navbar reste fonctionnelle.
### 5.3 Redimensionnement
- Si le chromeless retire la bordure de resize OS : ajouter de fines poignées CSS (bords + coins) qui
  pilotent le resize via l'hôte (`SetSize`/`SetLeft`+`SetTop`). Si l'OS conserve une bordure de resize en
  chromeless (à vérifier), ne rien ajouter.
### 5.4 État agrandi
- `html.is-maximized` → retire coins arrondis + ombre (sinon liseré visible). L'hôte notifie l'état au
  démarrage et à chaque changement.

## 6. Tests & vérification

- **bUnit** (`tests/Piscine.Components.Tests`) : `WindowControls` rend 3 boutons ; masqué quand le flag
  hôte ≠ photino ; aria-labels présents.
- **Playwright DevHost** (`tests/Piscine.DevHost.E2E`, port dédié **5281** — au-delà de la plage que la
  tâche de déduplication des ports pourrait réclamer, skip-sans-Chromium) : la barre
  de titre rend, **les contrôles de fenêtre sont masqués dans le navigateur**, la navigation marche encore,
  la zone drag ne casse pas les clics nav.
- **Smoke de rendu Photino (local, Windows)** : la fenêtre rend **chromeless** (pas de chrome OS),
  `received:true`, `#app` non vide, contrôles présents. Vérif manuelle : drag, réduire, agrandir/restaurer,
  fermer, resize. (Le harnais smoke a un faux négatif d'arrêt connu sous Windows → lire le bilan JSON
  directement, cf. [[piscine-qol-epic]].)
- **Linux** : vérifier le rendu chromeless + drag via Docker (WebKitGTK + xvfb) si faisable ; sinon valider
  le **repli** (chrome OS conservé) et documenter la vérif AppImage manuelle.
- **Qualité** : `dotnet build/test Piscine.slnx -c Release` **0 warning** ; les deux hôtes compilent.

## 7. Release v4.0.0 (après merge du chrome)

1. Bump de version (là où la version est définie : `Directory.Build.props`/csproj/`release.yml` — repérer
   la source) vers **4.0.0**.
2. `CHANGELOG.md` : promouvoir la section « Non publié » en **`[v4.0.0]`** datée, résumant **l'épic QoL
   desktop S1–S8** (tableau de bord, plan de travail + « Ouvrir », palette ⌘K + recherche, retour enrichi
   diff+toast, rapport+export, réglages thème/police, a11y+onboarding, docs) **+ le chrome de fenêtre custom**.
3. `main` vert + dry-runs `release.yml` (installeurs Win + AppImage Linux) OK.
4. **Créer & pousser le tag `v4.0.0`** → `release.yml` construit et publie les installeurs/artefacts.
   (Décision proprio : « full release, je tague & publie ».)
5. Vérifier le run `release.yml` vert et les artefacts publiés.

## 8. Risques

- **Drag/resize cross-plateforme** (WebKitGTK) = risque #1 → repli OS-chrome Linux prévu.
- **Surface d'API PhotinoX** (noms exacts chromeless/min/max/move) → étape de vérif au plan.
- **Régression écran noir** : tout changement de l'hôte Photino doit repasser le smoke de rendu (gate local).
- **v4.0.0 = release majeure** : publication outward-facing irréversible → ne taguer qu'après main vert +
  dry-runs verts.

## 9. Invariants (rappel)

UI → `Piscine.Components` ; pilotage fenêtre → `Piscine.Desktop` (DevHost no-op). DI éventuelle dans les
deux hôtes. Moteur/CLI/grade-received/`release.yml` (logique) intacts. Commits conventionnels FR + trailer
`Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`. commit ≠ push.
