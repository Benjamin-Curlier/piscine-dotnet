# Plan — v5 S13 : installeurs Windows + Linux, abandon macOS, install indépendant

> Issue **#46** (milestone #3 « v5 », label `v5`). Branche : `v5/s13-packaging-strategy`.
> **Décision proprio** : [`../adr/2026-06-07-packaging-zip-vs-installeur.md`](../adr/2026-06-07-packaging-zip-vs-installeur.md)
> (option C adaptée : installeurs Win+Linux, **macOS abandonné**, runtime+git bundlés = indépendant ;
> **PAS** le SDK .NET — self-contained + Roslyn embarqué suffisent). **Aucun tag** (release = proprio).

## Objectif
Remplacer/compléter les zips par des **installeurs indépendants et 100% HORS-LIGNE** : Windows **Inno
Setup (.exe)** et Linux **AppImage**, chacun embarquant tout le nécessaire (app desktop + CLI `piscine` +
`content/` + git + shim + **runtime webview**). Abandonner `osx-arm64`. Le poste recrue n'installe **rien**
et **n'a besoin d'aucun accès internet** — ni à l'installation, ni à l'usage (correction Roslyn embarquée,
runtime .NET self-contained, git bundlé, webview bundlé).

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
- [ ] **WebView2 HORS-LIGNE = Fixed Version** (PAS le bootstrapper Evergreen qui télécharge) : au build CI,
  télécharger le **runtime WebView2 Fixed Version** (paquet versionné Microsoft, ~120–180 Mo), l'extraire
  dans le payload sous `webview2/`. L'installeur le copie ; le lanceur desktop exporte
  `WEBVIEW2_BROWSER_EXECUTABLE_FOLDER=<install>\webview2\...` avant de lancer l'app (le loader WebView2
  honore cette variable ; sans admin, hors-ligne). Vérifier que Photino démarre avec cette variable pointée
  (smoke local : poser un FV extrait + lancer l'exe avec la variable → 0 crash).
- [ ] `release.yml` : **nouveau job `release-windows` (runs-on: windows-latest)** : setup-dotnet ;
  `package-content` ; publish win-x64 (CLI+desktop+gitshim) ; télécharger MinGit + **WebView2 Fixed Version** ;
  `choco install innosetup` ; `ISCC piscine.iss` → `dist/piscine-<tag>-win-x64-setup.exe` ; upload à la
  release. (Installeur volumineux = assumé pour l'hors-ligne.)
- [ ] Commit : `build(release): installeur Windows Inno Setup (app+CLI+content+MinGit+WebView2 bootstrapper)`

## T3 — Installeur Linux (AppImage, indépendant)
- [ ] `build/installer/linux/` : `AppDir/AppRun` (lance `desktop/Piscine.Desktop`, met `gitshim`+git sur
  PATH, exporte les libs bundlées), `piscine.desktop`, icône. **git embarqué** (binaire statique/portable
  Linux sous l'AppDir). **`libwebkit2gtk-4.1` + dépendances embarquées OBLIGATOIRE** (hors-ligne interdit
  `apt`) via **`linuxdeploy` + `linuxdeploy-plugin-gtk`** (bundle webkit2gtk/gtk/gdk-pixbuf/loaders).
- [ ] `release.yml` (job ubuntu) : publish linux-x64 self-contained (CLI+desktop+gitshim) → AppDir ;
  installer les libs sur le runner (apt) pour que `linuxdeploy` les **copie dans l'AppImage** ;
  `appimagetool` → `dist/piscine-<tag>-linux-x86_64.AppImage` ; upload. (AppImage volumineux = assumé.)
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
