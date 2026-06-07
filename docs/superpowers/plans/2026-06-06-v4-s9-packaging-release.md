# Plan — v4 S9 : packaging/release Photino + docs setup webview

> Issue **#30** (milestone #2 « v4 — application desktop Photino », label `v4`).
> Branche : `v4/s9-packaging-release`. Dépendances : **S1–S8 mergées** (`main`, 246 tests verts).
> Spec : [`../specs/2026-06-06-v4-photino-desktop-design.md`](../specs/2026-06-06-v4-photino-desktop-design.md).
> HANDOFF : [`../HANDOFF.md`](../HANDOFF.md) (« Pièges v4 », DÉCISIONS).

## Décisions de tête (recherche faite le 2026-06-06)

- **(a) Insertion dans le zip + OutputType.** `release.yml` boucle `for rid in linux-x64 win-x64 osx-arm64`
  et publie `Piscine.Cli` self-contained par RID dans `$out`, copie `content/`, MinGit+`start-piscine.cmd`
  (win), zippe `piscine-$rid`. S9 ajoute, dans la **même** itération, un publish de `src/Piscine.Desktop`
  dans `$out/desktop/` + un lanceur par OS. Exe = `Piscine.Desktop(.exe)` (pas d'`AssemblyName` override →
  pas de collision avec le CLI `piscine`). **`OutputType=WinExe` conservé** : flag de sous-système PE,
  ignoré hors Windows ; T1 confirme le publish cross-RID (repli `OutputType` conditionnel **seulement si**
  un RID non-Windows échoue). Libs natives **vérifiées** (Photino.Native 3.2.3) : publish self-contained par
  RID amène `runtimes/<rid>/native/` = `Photino.Native.dll`+`WebView2Loader.dll` (win), `.so` (linux),
  `.dylib` (osx). `Piscine.DevHost` **exclu par construction** (jamais nommé dans un publish).
- **(b) Vérification SANS tag** (la job release ne tourne qu'au tag) : (1) **smoke local win-x64** (T1 :
  publish → assert exe + libs natives) ; (2) **dry-run CI** (T3 : publish des 3 RID sur ubuntu + assert lib
  native par RID, à **chaque PR**) = la même opération que la release, exercée avant tout tag. Validation
  finale par OS = checklist proprio après tag d'une **pré-release** (non agent-vérifiable : fenêtre native).
- **(c) Top 3 risques** : 1) publish cross-RID Photino depuis ubuntu (`WinExe` + libs natives) → T1 smoke +
  T3 dry-run 3 RID + repli conditionnel ; 2) runtime webview par OS (Linux `libwebkit2gtk-4.1` ; Windows
  WebView2, absent des éditions N ; macOS WKWebView intégré) → checklist docs (T4) ; 3) packaging qui « rote »
  entre deux sprints (release au tag seulement) → le dry-run CI (T3) le surveille sur chaque PR.

## Objectif

Livrer l'app desktop **`Piscine.Desktop`** dans les zips par OS produits par `release.yml`, à côté du CLI
`piscine` **toujours livré** (le hook l'appelle), du `content/` et de MinGit (Windows). Documenter le
**setup webview** par OS + une **checklist smoke manuelle** par OS. Le sprint livre la *capacité* de
packaging ; le proprio taguera une **pré-release** pour valider.

## ⚠️ Risques & garde-fous

**SPRINT DIFFÉRENT — `release.yml` est MODIFIÉ LÉGITIMEMENT** (c'est l'objet du sprint). L'invariant
« release.yml intact » de S1–S8 est **levé pour S9**. **NOUVELLE garde S9** :
- **NE PAS toucher le code SOURCE du moteur** : `src/Piscine.Core`, `src/Piscine.Grading`, `src/Piscine.Git`,
  `src/Piscine.Cli` (comportement inchangé ; seul leur *packaging* évolue).
- Modifs autorisées : `release.yml`, `ci.yml`, `docs/**`, `build/launchers/**`, et **seulement si nécessaire**
  `src/Piscine.Desktop/Piscine.Desktop.csproj` (condition `OutputType` — c'est l'app desktop, permis).
- **Build solution 0 warning** (WarningsAsErrors) ; **246 tests verts** ; `validate-content` OK.
- **NE PAS pousser de tag** (release publique = action du proprio, cf. DÉCISIONS du HANDOFF).

(Détails risques principal/secondaire : cf. Décisions (c).)

## Carte des fichiers

| Fichier | Action |
|---|---|
| `src/Piscine.Desktop/Piscine.Desktop.csproj` | T1 — `OutputType` conditionnel **seulement si** le publish non-Windows échoue |
| `.github/workflows/release.yml` | T2 — publier `Piscine.Desktop` par RID dans `desktop/` + lanceurs (DevHost exclu, CLI intact) |
| `build/launchers/start-piscine-desktop.cmd` | T2 — lanceur desktop Windows (nouveau) |
| `build/launchers/start-piscine-desktop.sh` | T2 — lanceur desktop Linux/macOS (nouveau) |
| `.github/workflows/ci.yml` | T3 — dry-run publish desktop par RID + assert libs natives présentes |
| `docs/deploiement.md` | T4 — contenu du zip (desktop), setup webview, répétition locale, smoke par OS |
| `docs/mise-en-oeuvre.md` | T4 — lancement de l'app desktop côté recrue + prérequis webview |
| `docs/superpowers/retex/2026-06-06-v4-s9-packaging-release.md` | T5 — retex + checklist smoke par OS (proprio) |

---

## T1 — Résoudre le packaging de `Piscine.Desktop` + smoke publish local

**But** : confirmer qu'un publish self-contained produit l'exe + les libs natives ; décider du sort de `OutputType=WinExe`.
**Fichiers** : `src/Piscine.Desktop/Piscine.Desktop.csproj` (conditionnel **seulement si** nécessaire).

- [ ] **Step 1 — Smoke publish win-x64** :
  `dotnet publish src/Piscine.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/desktop-smoke/win-x64`
- [ ] **Step 2 — Assert (lecture)** : `Piscine.Desktop.exe` + `runtimes/win-x64/native/Photino.Native.dll` + `.../WebView2Loader.dll` présents.
- [ ] **Step 3 — Tester un RID non-Windows** (valider `WinExe` cross-RID AVANT de toucher release.yml) :
  `dotnet publish src/Piscine.Desktop -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/desktop-smoke/linux-x64` → assert `runtimes/linux-x64/native/Photino.Native.so`.
- [ ] **Step 4 — Décision** : si le publish linux **réussit** (attendu) → **ne pas modifier le csproj**.
  Si **échec** sur `OutputType` → rendre conditionnel :
  ```xml
  <OutputType Condition="'$(RuntimeIdentifier)' == 'win-x64'">WinExe</OutputType>
  <OutputType Condition="'$(RuntimeIdentifier)' != 'win-x64'">Exe</OutputType>
  ```
- [ ] **Step 5 — Build solution** : `dotnet build Piscine.slnx -c Release` → 0 warning.

Expected : exe + libs natives présents (win-x64 ET linux-x64) ; build solution 0 warning. Fenêtre native NON vérifiée par l'agent.
Commit (seulement si csproj changé) : `build(desktop): OutputType conditionnel par RID pour publish cross-RID`

---

## T2 — Étendre `release.yml` : empaqueter l'app desktop par RID

**Fichiers** : `.github/workflows/release.yml`, `build/launchers/start-piscine-desktop.{cmd,sh}`.

- [ ] **Step 1 — `build/launchers/start-piscine-desktop.cmd`** (Windows ; calqué sur start-piscine.cmd, PATH MinGit + lance l'app) :
  ```bat
  @echo off
  rem Lanceur Piscine Desktop (Windows) : place git portable (MinGit) sur le PATH puis lance l'app.
  set "PISCINE_DIR=%~dp0"
  set "PATH=%PISCINE_DIR%mingit\cmd;%PISCINE_DIR%;%PATH%"
  start "" "%PISCINE_DIR%desktop\Piscine.Desktop.exe"
  ```
- [ ] **Step 2 — `build/launchers/start-piscine-desktop.sh`** (Linux/macOS) :
  ```sh
  #!/bin/sh
  # Lanceur Piscine Desktop (Linux/macOS). Prerequis webview : voir docs/mise-en-oeuvre.md.
  DIR="$(cd "$(dirname "$0")" && pwd)"
  exec "$DIR/desktop/Piscine.Desktop"
  ```
- [ ] **Step 3 — `release.yml`** : dans la boucle, après le publish CLI + copie `content/`, AVANT le bloc `if win-x64`, ajouter :
  ```bash
  # App desktop Photino (self-contained par RID), a cote du CLI. DevHost JAMAIS empaquete.
  dotnet publish src/Piscine.Desktop --configuration Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$out/desktop"
  ```
- [ ] **Step 4 — Lanceurs par OS** (étendre le `if`) :
  ```bash
  if [ "$rid" = "win-x64" ]; then
    mkdir -p "$out/mingit"; unzip -q mingit.zip -d "$out/mingit"
    cp build/launchers/start-piscine.cmd "$out/start-piscine.cmd"
    cp build/launchers/start-piscine-desktop.cmd "$out/start-piscine-desktop.cmd"
  else
    cp build/launchers/start-piscine-desktop.sh "$out/start-piscine-desktop.sh"
    chmod +x "$out/start-piscine-desktop.sh"
  fi
  ```
- [ ] **Step 5 — Note release** (texte) : mentionner « CLI piscine + app desktop (dossier desktop/) ; prerequis webview docs/mise-en-oeuvre.md ».
- [ ] **Step 6 — Vérif statique** (pas de tag !) : `git diff` → confirmer DevHost absent, lignes CLI/content/MinGit inchangées.

Commit : `build(release): empaqueter l'app desktop Photino par RID (DevHost exclu, CLI intact)`

---

## T3 — Dry-run CI : publish desktop par RID + assert libs natives (sur PR)

**Fichiers** : `.github/workflows/ci.yml`.

- [ ] **Step 1 — Étape dry-run** (job `build-test`, ubuntu ; après « validate-content ») :
  ```yaml
  - name: Dry-run packaging desktop (publish cross-RID + libs natives)
    run: |
      set -euo pipefail
      for rid in linux-x64 win-x64 osx-arm64; do
        out="artifacts/desktop-dryrun/$rid"
        dotnet publish src/Piscine.Desktop --configuration Release -r "$rid" --self-contained true -p:PublishSingleFile=false -o "$out"
        test -f "$out/Piscine.Desktop" -o -f "$out/Piscine.Desktop.exe"
        case "$rid" in
          win-x64)   test -f "$out/runtimes/win-x64/native/Photino.Native.dll" && test -f "$out/runtimes/win-x64/native/WebView2Loader.dll" ;;
          linux-x64) test -f "$out/runtimes/linux-x64/native/Photino.Native.so" ;;
          osx-arm64) test -f "$out/runtimes/osx-arm64/native/Photino.Native.dylib" ;;
        esac
        echo "OK $rid"
      done
  ```
  (Décision RID : ubuntu cross-publie les 3 → dry-run **les 3**, le test le plus proche de la release réelle.)
- [ ] **Step 2 — Relire l'indentation YAML** (à défaut de linter).

Expected : au prochain push, la CI publie les 3 RID + asserte les libs natives → preuve automatique avant tout tag. Si `WinExe` casse un RID, T3 échoue ici (→ repli T1 step 4).
Commit : `ci: dry-run packaging desktop par RID (assert libs natives Photino) sur PR`

---

## T4 — Docs : setup webview + checklist smoke par OS

**Fichiers** : `docs/deploiement.md`, `docs/mise-en-oeuvre.md`.

- [ ] **Step 1 — `deploiement.md` §contenu du zip** : par RID, app desktop dans `desktop/` (self-contained, libs natives Photino) + lanceur `start-piscine-desktop.{cmd,sh}` ; `Piscine.DevHost` **jamais empaqueté**.
- [ ] **Step 2 — `deploiement.md` « Prérequis webview (par OS) »** :
  - **Windows** : runtime **WebView2** (préinstallé Win10 récent/Win11 ; **éditions N / images minimales** → installer l'Evergreen Runtime Microsoft ; `WebView2Loader.dll` dans le zip).
  - **Linux** : `libwebkit2gtk-4.1` — Debian/Ubuntu `sudo apt install libwebkit2gtk-4.1-0` ; Fedora `sudo dnf install webkit2gtk4.1`.
  - **macOS** : WKWebView **intégré**, rien à installer.
- [ ] **Step 3 — `deploiement.md` « Répétition locale »** : ajouter la commande desktop + l'assertion libs ; mentionner le dry-run CI 3 RID.
- [ ] **Step 4 — `deploiement.md` « Smoke pré-release par OS (action proprio) »** (checklist après tag pré-release + download par OS) :
  - [ ] Windows : dézipper → `start-piscine-desktop.cmd` → fenêtre + cours (titre + gras + bloc de code).
  - [ ] Linux : `libwebkit2gtk-4.1` installé → `./start-piscine-desktop.sh` → fenêtre + cours.
  - [ ] macOS : `./start-piscine-desktop.sh` → fenêtre + cours.
  - [ ] CLI **intact** : `piscine init` / `piscine status` répondent dans le même zip.
- [ ] **Step 5 — `mise-en-oeuvre.md`** (côté recrue) : prérequis webview par OS (renvoi deploiement.md) ; lancement de l'app desktop (`start-piscine-desktop.{cmd,sh}`) en plus du CLI ; contenu du zip (dossier `desktop/` + lanceur).
- [ ] **Step 6 — Garde-fou** : `validate-content` reste vert.

Commit : `docs(deploiement,mise-en-oeuvre): setup webview par OS + app desktop dans le zip + smoke pre-release`

---

## T5 — Vérification finale + retex + PR (sans tag)

- [ ] **Step 1** : `dotnet build Piscine.slnx -c Release` → 0 warning.
- [ ] **Step 2** : `dotnet test Piscine.slnx -c Release` → **246** verts.
- [ ] **Step 3** : `validate-content` → « Contenu valide. »
- [ ] **Step 4 — Garde « moteur source intact »** : `git diff --name-only origin/main...HEAD` ne touche AUCUN
  fichier sous `src/Piscine.Core|Grading|Git|Cli` (release.yml/ci.yml/docs/Desktop autorisés) :
  ```bash
  git diff --name-only origin/main...HEAD -- src/Piscine.Core src/Piscine.Grading src/Piscine.Git src/Piscine.Cli
  ```
  → **aucune ligne**.
- [ ] **Step 5 — Aucun tag** : `git tag --points-at HEAD` → vide.
- [ ] **Step 6 — Retex** `docs/superpowers/retex/2026-06-06-v4-s9-packaging-release.md` : décision `OutputType`,
  preuve libs natives par RID, dry-run CI comme filet, + **checklist smoke par OS (proprio)** recopiée de T4.
- [ ] **Step 7 — PR** (push + create, appels séparés ; **pas de tag**) :
  ```bash
  git push -u origin v4/s9-packaging-release
  gh pr create --base main --title "v4 S9 — packaging/release Photino + docs setup webview" --body-file <fichier>
  ```

Expected : build 0 warning, 246 tests verts, `validate-content` OK, **0 fichier moteur source modifié**, **aucun tag**, PR ouverte, CI verte (dry-run desktop inclus).
Commit (retex ; HANDOFF mis à jour par le parent au merge) : `docs(retex): consigner v4 S9 (packaging Photino par RID, dry-run CI, smoke proprio)`

---

## Self-review (couverture S9 vs issue #30)

- **Objectif** « livrer l'app desktop dans les zips par OS » → T2 (release.yml publie `desktop/` par RID + lanceurs). ✅
- **Périmètre** : `release.yml` ajoute `Piscine.Desktop` self-contained par RID (libs natives) à côté du CLI/content/MinGit ✅ T2 ; `Piscine.DevHost` **exclu** (jamais nommé) ✅ ; docs setup webview (WebView2/`libwebkit2gtk`/WKWebView) ✅ T4 ; checklist smoke par OS ✅ T4/T5.
- **Critères d'acceptation** : « un tag de pré-release produit des zips lançant l'app sur les 3 OS (smoke manuel) » → packaging livré + **dry-run CI 3 RID** (T3) prouve le publish ; smoke par OS = action proprio (checklist T4/T5) ✅ ; **CLI/hook intacts** (CLI publish inchangé ; garde diff source T5) ✅.
- **Dépendances** : S1–S8 (release de l'ensemble) ✅.
- **Gardes S9** : release.yml modifié **légitimement** ; **moteur source intact** (gate T5 Step 4) ; build 0 warning ; 246 tests ; **aucun tag** (T5 Step 5 ; release = action proprio).
- **Risque principal maîtrisé** : publish cross-RID + `WinExe` + libs natives → T1 smoke local + **T3 dry-run CI 3 RID** (preuve avant tag) + repli `OutputType` conditionnel documenté.
- **Pas de gold-plating** : pas de signature de code / notarisation / installeurs (hors périmètre) ; on livre des zips self-contained, comme le CLI.
