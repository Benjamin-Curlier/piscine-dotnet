# Retex — v5 S13 : installeurs Windows + Linux (offline & online), macOS abandonné

> Issue #46. Branche `v5/s13-packaging-strategy`. Plan : [../plans/2026-06-07-v5-s13-installers.md](../plans/2026-06-07-v5-s13-installers.md).
> ADR : [../adr/2026-06-07-packaging-zip-vs-installeur.md](../adr/2026-06-07-packaging-zip-vs-installeur.md).
> **Verdict : objectif atteint.** Installeurs **2 modes (offline/online) × 2 OS (Windows/Linux)** ;
> macOS abandonné ; zips conservés. Linux AppImage offline **prouvé hors-ligne en CI** ; installeur
> Windows **vérifié en local** (install/désinstall) **et** compilé en CI. **Aucun tag.**

## Ce qui a été fait
- **macOS abandonné** (`osx-arm64` retiré de release.yml + ci.yml).
- **Linux — AppImage** (`build/installer/linux/{AppRun,piscine.desktop,build-appimage.sh}`) :
  - **offline** : webkit2gtk-4.0 + gtk + deps + helpers bundlés (linuxdeploy + plugin gtk) ; git embarqué.
  - **online** : léger (webkit système).
- **Windows — Inno Setup** (`build/installer/windows/piscine.iss`) : installeur **per-utilisateur**
  (`PrivilegesRequired=lowest`), 2 modes via `ISCC /DMODE`. WebView2 : **offline = Standalone Evergreen**
  (full, hors-ligne) / **online = bootstrapper**, tous deux comme `webview2-setup.exe` lancé `/silent /install`
  **si WebView2 absent** (ignoré sinon — cas courant Win11/Win10 récents). Raccourcis menu Démarrer + Bureau.
- **`release.yml` (3 jobs)** : `package-linux` (ubuntu-22.04 : zips win+linux + AppImage offline/online) ·
  `package-windows` (windows-latest : choco innosetup + payload + ISCC ×2 + téléchargement WebView2/MinGit) ·
  `release` (agrège artefacts → `gh release create` zips + AppImage + setup.exe).
- **`ci.yml` (2 dry-runs ajoutés)** : `appimage-offline-dryrun` (build + lancement hors-ligne) +
  `windows-installer-dryrun` (Inno + ISCC, 2 modes, garde de compilation).

## Prouvé
- **CI verte (3 jobs)** : `build-test`, `appimage-offline-dryrun`, `windows-installer-dryrun`.
- **Linux offline = hors-ligne RÉEL** (CI, Docker `--network=none`, conteneur **sans** webkit + xvfb) :
  l'AppImage charge `app://localhost/` avec le webkit **bundlé**. (Aussi reproduit en local.)
- **Windows = vérifié en LOCAL** (Inno installé) : ISCC compile offline+online ; le offline **s'installe
  en silencieux sans admin**, pose la bonne arborescence, et **se désinstalle proprement**.
- **WebView2 fwlinks stables** vérifiées : offline `go.microsoft.com/fwlink/?linkid=2124701`
  (`MicrosoftEdgeWebView2RuntimeInstallerX64.exe`), online `.../p/?LinkId=2124703` (`MicrosoftEdgeWebview2Setup.exe`).
- **Garde moteur** : `git diff origin/main...HEAD -- src` = **vide** (aucun changement `src/` ; packaging seul).

## Constats clés (réutilisables)
- **Photino.Blazor 3.2.0 → webkit2gtk-4.0 (PAS 4.1).** Corrige les docs S9. Online Linux : `libwebkit2gtk-4.0-37`
  (apt) ; offline AppImage **bâti sur Ubuntu 22.04** (24.04 ne fournit plus 4.0).
- **WebView2 Fixed Version écarté** : >250 Mo, pas d'URL de DL stable (archive manuelle), `icacls` v120+,
  pas de UNC → non automatisable en CI. **Standalone Evergreen run-if-missing** = robuste + automatable.
- **AppImage + .NET self-contained** : retirer `libcoreclrtraceptprovider.so` (LTTng optionnel → `liblttng-ust.so.0`
  absent) ; l'AppImage s'appuie sur le baseline graphique desktop (X11/mesa/fontconfig) — normal (pas de GPU bundlé).
- **Bash `bash -c '...'` : zéro apostrophe** à l'intérieur (un `l'hôte` casse la chaîne → `syntax error )`).

## Checklist smoke par OS — **PROPRIO** (fenêtre native = non vérifiable par l'agent)
- [ ] **Windows offline** : `piscine-<v>-win-x64-offline-setup.exe` → installe (sans internet) → menu Démarrer
  « Piscine .NET » → fenêtre + cours ; `piscine` au terminal (start-piscine.cmd) ; `git push` corrige.
- [ ] **Windows online** : idem, machine sans WebView2 → l'installeur télécharge le runtime.
- [ ] **Linux offline** : `*-offline.AppImage` sur une machine **sans** `libwebkit2gtk-4.0` et **hors-ligne**
  → fenêtre + cours.
- [ ] **Linux online** : `*-online.AppImage` (webkit système / `apt install libwebkit2gtk-4.0-37`).

## Suites (hors S13)
- Docs (S14 #47) : corriger webkit2gtk **4.0**, documenter installeurs/terminal-in-app/résultat-riche, macOS abandonné.
- Terminal embarqué dans l'AppImage : nécessite un git **stable** pour le hook (mount AppImage éphémère) —
  à traiter si le rendu desktop Linux passe au terminal in-app (aujourd'hui : `git push` au terminal système).
