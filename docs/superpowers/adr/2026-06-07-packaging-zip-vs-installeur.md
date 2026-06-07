# ADR — Stratégie de packaging : zip self-contained vs installeur (v5 S13, #46)

> Statut : **proposé — décision proprio en attente.** Date : 2026-06-07.
> Contexte : la piscine livre désormais une **app de bureau Photino** (v4/v5) en plus du CLI `piscine`.
> On réévalue le mode de distribution (aujourd'hui : **zip self-contained par OS**).

## Contexte
- **Distribution actuelle** (`release.yml`, tag `v*`) : 3 zips self-contained (`win-x64`/`linux-x64`/
  `osx-arm64`). Chaque zip = CLI `piscine` + `content/` + app `desktop/` (+ `gitshim/` depuis S12) +
  lanceurs + MinGit (Windows). **Sans SDK, sans admin, portable.**
- **Public & canal** : bootcamp **interne** ; remise par **clé USB / partage interne** (cf. design),
  recrues souvent débutantes, encadrant présent.
- **Nouveau besoin (app de bureau)** : **runtime webview** par OS — WebView2 (Windows ; absent des éditions
  N / images minimales), `libwebkit2gtk-4.1` (Linux), WKWebView (macOS, intégré). Aujourd'hui = **prérequis
  manuel documenté**.
- **Contrainte assumée du projet** : binaires **non signés** (SmartScreen/Gatekeeper tolérés, documentés).
  Pas de compte signature/notarisation.

## Ce qu'un installeur apporterait (et son coût)
| Piste | Gain | Coût / risque |
|---|---|---|
| **Windows — Inno Setup (.exe)** | raccourci menu Démarrer ; **bootstrapper WebView2 Evergreen** intégré (règle le prérequis sur éditions N) ; désinstallation propre | build CI supplémentaire ; **non signé → SmartScreen** persiste ; installe (droits utilisateur OK) |
| **Windows — MSIX** | moderne, sandbox, MAJ | **exige une signature** pour s'installer → bloquant sans certificat |
| **Linux — .deb/.rpm** | **déclare `libwebkit2gtk-4.1`** (résout le prérequis via apt/dnf) ; intégration menu | packaging **par famille de distro** ; CI plus lourde ; suppose apt/dnf (pas universel) |
| **Linux — AppImage** | **portable** (un fichier exécutable), embarque les deps | bundling `libwebkit2gtk` **délicat** ; toujours non signé |
| **macOS — .app/.dmg** | UX native | **notarisation Gatekeeper = compte Apple Developer (99 $/an)** sinon « app endommagée » ; lourd pour un usage interne |

## Analyse
- Le **zip self-contained** colle au canal réel (USB/partage interne, pas d'app store, encadrant présent) :
  zéro admin, zéro dépendance d'install, **réversible**, déjà éprouvé + **dry-run CI 3 RID**.
- Le **seul vrai point douloureux** que l'app de bureau ajoute = le **runtime webview** ; un installeur
  ne « vaut le coup » surtout pour **l'auto-installer**. Mais :
  - **macOS** : sans notarisation (99 $/an), un .dmg n'améliore pas l'UX (Gatekeeper bloque autant qu'un zip) → **non rentable**.
  - **Linux** : `.deb` résout le prérequis mais coûte un packaging par distro ; l'alternative **une ligne
    `apt/dnf`** documentée est quasi aussi simple pour un public encadré.
  - **Windows** : c'est là que l'installeur a le **meilleur rapport** (raccourci + bootstrapper WebView2),
    et **Inno Setup** ne requiert **pas** de signature pour *produire* l'installeur (SmartScreen reste,
    comme aujourd'hui).

## Options pour la décision
- **(A) Garder le zip + améliorations ciblées _(recommandé)_** : rester en zip (simple/portable), et
  réduire la friction webview **sans installeur** — p.ex. inclure le **bootstrapper WebView2** dans le zip
  Windows + un check dans `start-piscine-desktop.cmd` (lance le bootstrapper si WebView2 absent), et garder
  la **doc** apt/dnf pour Linux. Faible coût, réversible, aucun compte de signature. *(Implémentable en S13.)*
- **(B) Ajouter un installeur Windows (Inno Setup) en plus des zips** : meilleur confort Windows (raccourci
  + WebView2), Linux/macOS restent en zip. Coût CI modéré ; non signé (SmartScreen inchangé).
- **(C) Matrice d'installeurs complète (.exe + .deb/AppImage + .dmg)** : UX maximale mais **coût élevé**
  (packaging par OS, et **macOS exige la notarisation payante** pour valoir le coup). Le moins réversible.

## Recommandation
**(A)** pour ce sprint : le zip reste le bon défaut pour une distribution **interne USB/partage**, et la
seule douleur réelle (webview) se traite à moindre coût. Réserver **(B)** si l'on veut polir Windows plus
tard ; **(C)** seulement si la distribution devient publique/large (et avec budget signature).

## Décision (proprio, 2026-06-07)
**Option (C) adaptée — installeurs Windows + Linux, macOS abandonné, environnement prêt et 100% HORS-LIGNE.**

> **EXIGENCE CLÉ (proprio)** : livrer, par OS, **DEUX modes d'installeur** :
> - **OFFLINE** — environnement prêt qui fonctionne **SANS INTERNET** (install + usage) : **tout embarqué**
>   (runtime, git, **runtime webview**, contenu). Aucun téléchargement sur le poste recrue.
> - **ONLINE** — installeur **plus léger** qui **récupère le runtime webview à l'installation** (le reste —
>   app, CLI, `content/`, git, shim — reste embarqué dans les deux modes ; seul le **webview** diffère).
>
> Les deux modes sont produits par OS. La différence = **uniquement le runtime webview** (embarqué vs récupéré).

- **Abandonner la cible macOS** (`osx-arm64`) : retirée de `release.yml` + dry-run `ci.yml` + docs.
- **Installeurs par OS** (le livrable principal), **2 variantes (offline / online)** :
  - **Windows** : installeur **Inno Setup** (`.exe`) bâti dans un **job runner `windows-latest`** ;
    embarque app desktop + CLI `piscine` + `content/` + **MinGit** + raccourci dans **les deux modes**.
    WebView2 : **offline** = runtime **« Fixed Version »** embarqué (lanceur pointe
    `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER=<install>/webview2`, sans admin) ; **online** = **bootstrapper
    Evergreen** exécuté à l'install (télécharge WebView2). 2 sorties : `…-win-x64-offline-setup.exe` /
    `…-win-x64-online-setup.exe` (même `.iss`, define ISCC `/DMODE`).
  - **Linux** : **AppImage** embarquant app self-contained + **git** dans les deux modes. webkit :
    **offline** = **`libwebkit2gtk-4.1` + deps embarqués** (`linuxdeploy`+plugin gtk) ; **online** = AppImage
    léger s'appuyant sur le **webkit système** (sinon `apt install libwebkit2gtk-4.1-0`). 2 sorties :
    `…-linux-x86_64-offline.AppImage` / `…-linux-x86_64-online.AppImage`.
- **« .NET SDK »** : interprété comme **runtime .NET embarqué** (publish **self-contained**, déjà le cas) +
  **Roslyn embarqué** pour la correction → **aucun SDK .NET requis** côté recrue. **On NE bundle PAS le SDK**
  (≈ Go inutiles, contraire au design « sans SDK »). *(Si le proprio voulait littéralement le SDK, me le dire.)*
- **git** : bundlé pour être indépendant — Windows via MinGit (déjà) ; Linux via git embarqué dans l'AppImage.
- **Zips** : conservés en parallèle (repli portable, faible coût) sauf avis contraire.
- **Aucun tag** poussé par la boucle (release publique = action proprio).

> Réalisation **étagée** (gros chantier CI) : T1 drop macOS, T2 installeur Windows (Inno + runner Windows),
> T3 AppImage Linux (+ git), T4 dry-run CI des installeurs, T5 docs (S14). Vérif Linux via Docker ;
> installeur Windows vérifié par le job CI ; install « réel » par OS = smoke proprio.
