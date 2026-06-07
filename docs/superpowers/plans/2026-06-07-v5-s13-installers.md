# Plan — v5 S13 : installeurs Windows + Linux, abandon macOS, install indépendant

> Issue **#46** (milestone #3 « v5 », label `v5`). Branche : `v5/s13-packaging-strategy`.
> **Décision proprio** : [`../adr/2026-06-07-packaging-zip-vs-installeur.md`](../adr/2026-06-07-packaging-zip-vs-installeur.md)
> (option C adaptée : installeurs Win+Linux, **macOS abandonné**, runtime+git bundlés = indépendant ;
> **PAS** le SDK .NET — self-contained + Roslyn embarqué suffisent). **Aucun tag** (release = proprio).

## Objectif
Remplacer/compléter les zips par des **installeurs** : Windows **Inno Setup (.exe)** et Linux **AppImage**,
**en DEUX modes par OS** :
- **OFFLINE** : tout embarqué (app desktop + CLI `piscine` + `content/` + git + shim + **runtime webview**)
  → fonctionne **sans internet** (install + usage).
- **ONLINE** : plus léger, **récupère le runtime webview à l'installation** (le reste reste embarqué).

App/CLI/content/git/shim/runtime .NET (self-contained) + Roslyn embarqué sont **toujours** embarqués ;
**seul le webview diffère** entre les deux modes. Abandonner `osx-arm64`.

## ⚠️ Ampleur & garde-fous
- **Gros chantier CI** (nouveau job runner Windows, nouveaux outils d'empaquetage) → **étagé** ; commits
  par tâche ; CI verte à chaque étape.
- **NE PAS** toucher le moteur de notation (Grading) ni la logique CLI ; on change l'**empaquetage**.
- **Vérif** : Linux via **Docker** ; build installeur Windows via le **job CI Windows** ; dry-run CI des
  installeurs sur PR si raisonnable ; l'install « réel » par OS = **smoke proprio**.
- **Aucun tag**. Zips conservés en parallèle (repli) sauf avis contraire.

## Carte des fichiers
| Fichier | Action |
|---|---|
| `.github/workflows/release.yml` | T1 drop osx ; T2 job Windows (Inno) ; T3 step AppImage Linux |
| `.github/workflows/ci.yml` | T1 drop osx du dry-run ; T4 dry-run build installeurs (au moins compile `.iss` + AppImage) |
| `build/installer/windows/piscine.iss` (neuf) | T2 script Inno Setup (fichiers, raccourci, bootstrapper WebView2) |
| `build/installer/windows/*` | T2 helper (ex. lance MicrosoftEdgeWebview2Setup si WebView2 absent) |
| `build/installer/linux/` (neuf) | T3 AppDir (`AppRun`, `.desktop`, icône) + recette AppImage |
| `docs/deploiement.md`, `docs/mise-en-oeuvre.md` | T5/S14 — refléter installeurs + drop macOS |

---

## T1 — Abandonner macOS (rapide, vérifiable)
- [ ] `release.yml` : boucle RID → **`for rid in linux-x64 win-x64`** (retirer `osx-arm64`). Retirer le
  cas osx des notes.
- [ ] `ci.yml` : dry-run desktop → retirer `osx-arm64` (et son assert `.dylib`).
- [ ] Build/validate OK. Commit : `build(release,ci): abandonner la cible macOS (osx-arm64)`

## T2 — Installeur Windows (Inno Setup, job runner Windows)
- [ ] `build/installer/windows/piscine.iss` : Inno Setup. Source = un dossier `payload/` assemblé
  (publish win-x64 self-contained du CLI + desktop + gitshim, `content/`, MinGit, lanceurs). Crée
  raccourcis (menu Démarrer : « Piscine .NET » → desktop ; « Piscine (terminal) » → start-piscine.cmd),
  installe sous `{autopf}\Piscine .NET` ou `{localappdata}` (sans admin si possible : `PrivilegesRequired=lowest`).
- [ ] **Un seul `.iss`, 2 modes via define ISCC `/DMODE=offline|online`** (commun : app desktop + CLI +
  `content/` + MinGit + raccourcis ; `PrivilegesRequired=lowest`) :
  - **offline** : embarque le **runtime WebView2 Fixed Version** (téléchargé au build CI, ~120–180 Mo,
    extrait sous `webview2/`) ; le lanceur desktop exporte
    `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER=<install>\webview2\…` avant de lancer l'app (loader honore la
    variable ; sans admin ; **hors-ligne**). Smoke local : FV extrait + variable → 0 crash.
  - **online** : embarque le **bootstrapper Evergreen** (`MicrosoftEdgeWebview2Setup.exe`) ; étape
    `[Run]`/`[Code]` qui l'exécute **si WebView2 absent** (clé registre `pv` sous
    `…\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}`) → **télécharge** WebView2. Plus léger.
- [ ] `release.yml` : **job `release-windows` (windows-latest)** : setup-dotnet ; `package-content` ;
  publish win-x64 (CLI+desktop+gitshim) ; télécharger MinGit + **WebView2 Fixed Version** + bootstrapper ;
  `choco install innosetup` ; **2 builds** `ISCC /DMODE=offline` et `/DMODE=online` →
  `dist/piscine-<tag>-win-x64-offline-setup.exe` et `…-online-setup.exe` ; upload.
- [ ] Commit : `build(release): installeur Windows Inno Setup (app+CLI+content+MinGit+WebView2 bootstrapper)`

## T3 — Installeur Linux (AppImage, indépendant)
- [ ] `build/installer/linux/` : `AppDir/AppRun` (lance `desktop/Piscine.Desktop`, met `gitshim`+git sur
  PATH, exporte les libs bundlées le cas échéant), `piscine.desktop`, icône. **git embarqué** (binaire
  statique/portable Linux) dans **les deux modes**. **2 modes** :
  - **offline** : **`libwebkit2gtk-4.1` + deps embarqués** via **`linuxdeploy` + `linuxdeploy-plugin-gtk`**
    (bundle webkit2gtk/gtk/gdk-pixbuf/loaders) → fonctionne sans `apt`.
  - **online** : AppImage **léger** sans webkit bundlé → s'appuie sur le **webkit système** (sinon
    `apt install libwebkit2gtk-4.1-0` à l'install).
- [ ] `release.yml` (job ubuntu) : publish linux-x64 (CLI+desktop+gitshim) → AppDir ; **2 sorties** :
  `appimagetool` direct (online) et via `linuxdeploy`+gtk (offline) →
  `dist/piscine-<tag>-linux-x86_64-online.AppImage` et `…-offline.AppImage` ; upload. (Offline volumineux = assumé.)
- [ ] **Vérif Docker** : monter/extraire l'AppDir et lancer le CLI + tester le PTY dans
  `dotnet/sdk:10.0` (la fenêtre AppImage = proprio ; mais le CLI + PtyService testables headless).
- [ ] Commit : `build(release): AppImage Linux (app+CLI+content+git+shim, independant)`

## T4 — Dry-run CI des installeurs (filet avant tag)
- [ ] `ci.yml` : sur PR, au moins **compiler le `.iss`** (job windows : choco innosetup + ISCC sur un
  payload minimal) et **assembler l'AppImage** (ubuntu) → attrape les régressions d'empaquetage avant tag.
  (Si trop lourd pour chaque PR : limiter au `release.yml` + un smoke réduit.)
- [ ] Commit : `ci: dry-run de build des installeurs (Inno + AppImage)`

## T5 — Vérif globale + retex + (docs → S14)
- [ ] Build solution 0 warning ; `dotnet test Piscine.slnx -c Release` verts ; `validate-content` OK.
- [ ] Garde : Grading/CLI/Core intacts (`git diff origin/main...HEAD -- src/Piscine.Grading src/Piscine.Cli src/Piscine.Core` vide).
- [ ] Retex (décisions, ce qui est prouvé en CI/Docker, checklist install proprio par OS) + PR
  (push/create séparés) → CI verte → squash-merge `Fixes #46` → consigner. Docs détaillées = **S14**.

Expected : `release.yml` produit un **installeur Windows (.exe)** + un **AppImage Linux**, indépendants
(runtime+git+webview gérés), **plus de macOS** ; CI verte ; Grading/CLI intacts ; aucun tag. La doc suit en S14.

## Vérification de l'exigence HORS-LIGNE
- **Windows** : smoke local du FV WebView2 (`WEBVIEW2_BROWSER_EXECUTABLE_FOLDER` → app démarre, 0 crash) ;
  build installeur en CI ; install + lancement **sans réseau** = smoke proprio (couper le réseau).
- **Linux** : via **Docker** — exécuter l'AppImage extrait (`--appimage-extract`) dans un conteneur **minimal
  SANS internet** (`docker run --network=none`) et vérifier que le CLI + PtyService tournent (la fenêtre
  webview = proprio, mais l'absence des libs webkit bundlées se verrait via `ldd`/lancement). Confirme que
  rien n'est tiré du réseau.

## Notes / risques
- **Inno non préinstallé** sur les runners GitHub → `choco install innosetup`. Build installeur **non signé**
  (SmartScreen persiste, comme aujourd'hui — assumé). Ces téléchargements/outillages sont au **build CI**
  (avec internet) ; le **poste recrue** reste 100% hors-ligne.
- **AppImage + webkit OBLIGATOIRE** (hors-ligne) : `linuxdeploy-plugin-gtk` bundle webkit2gtk/gtk/loaders ;
  fiabiliser via `ldd` sur le binaire dans l'AppImage + lancement `--network=none`. C'est le **point dur**
  du sprint → si le bundling webkit résiste, le remonter au proprio (pas de repli « apt » : ce serait violer
  l'exigence hors-ligne).
- **Taille** : FV WebView2 (~150 Mo) + webkit bundlé gonflent les installeurs → **assumé** (prix du hors-ligne).
- **Zips** conservés (repli) ; si le proprio veut les retirer, le faire en T1.
