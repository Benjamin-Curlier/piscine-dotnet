# Plan d'implémentation — Migration PhotinoX (`Photino.Blazor 3.2.0` → `PhotinoX.Blazor 4.2.0`)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrer l'hôte de bureau `Piscine.Desktop` du paquet `Photino.Blazor 3.2.0` vers le fork maintenu `PhotinoX.Blazor 4.2.0` (net10-natif), puis publier la release **v3.1.0** via le flux CI standard.

**Architecture:** Swap de dépendance NuGet + retrait de l'épingle WebView NU1605. PhotinoX conserve l'API (`namespace Photino.Blazor`, `PhotinoBlazorAppBuilder`) → **aucun code applicatif à changer**. Ripple : les libs natives sont renommées `PhotinoX.Native.{dll,so}` (Windows garde `WebView2Loader.dll`) et Linux passe à **webkit2gtk-4.1** → maj des assertions CI, du runner Linux (`ubuntu-22.04` → `ubuntu-24.04`), du script AppImage et des docs.

**Tech Stack:** .NET 10 (SDK 10.0.x), `Piscine.slnx`, GitHub Actions (`ci.yml`/`release.yml`), Inno Setup (Windows), linuxdeploy/AppImage (Linux), `gh` CLI.

**Référence des noms natifs (vérifiés dans le nupkg PhotinoX.Native 4.2.0) :**
- `runtimes/win-x64/native/PhotinoX.Native.dll` + `runtimes/win-x64/native/WebView2Loader.dll`
- `runtimes/linux-x64/native/PhotinoX.Native.so`
- (le publish self-contained **aplatit** ces fichiers à la racine du dossier de sortie)

---

## Préliminaire

Branche déjà créée : `feat/photinox-migration` (le spec y est commité). Toutes les commandes partent de `E:\forgejo\piscine-dotnet`.

---

### Task 1 : Swap du paquet + retrait de l'épingle WebView ; build & namespace vérifiés

**Files:**
- Modify: `src/Piscine.Desktop/Piscine.Desktop.csproj`

- [ ] **Step 1 : Remplacer le bloc `<ItemGroup>` des PackageReference**

Remplacer EXACTEMENT ce bloc :

```xml
  <ItemGroup>
    <!-- Photino.Blazor 3.x (sur Photino.NET v3) : couche Blazor (PhotinoBlazorAppBuilder). -->
    <PackageReference Include="Photino.Blazor" Version="3.2.0" />
    <!-- Aligne Microsoft.AspNetCore.Components.WebView (tiré par Photino.Blazor en 8.0.x) sur le
         framework partagé net10.0 installé, sinon NU1605 (downgrade) sous TreatWarningsAsErrors. -->
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView" Version="10.0.8" />
  </ItemGroup>
```

par :

```xml
  <ItemGroup>
    <!-- PhotinoX.Blazor 4.x (fork maintenu net10-natif de Photino.Blazor) : couche Blazor
         (PhotinoBlazorAppBuilder, namespace Photino.Blazor conservé). Aligne nativement
         Microsoft.AspNetCore.Components.WebView sur 10.0.x → plus d'épingle NU1605 nécessaire. -->
    <PackageReference Include="PhotinoX.Blazor" Version="4.2.0" />
  </ItemGroup>
```

- [ ] **Step 2 : Restaurer et builder le projet desktop**

Run : `dotnet build src/Piscine.Desktop/Piscine.Desktop.csproj -c Release`
Expected : **Build succeeded, 0 Warning, 0 Error** (sous `TreatWarningsAsErrors` → prouve que l'épingle WebView est superflue et qu'aucun NU1605/NU1701 ne réapparaît).

**Contingence** (si erreur `CS0246: namespace 'Photino' / type 'PhotinoBlazorAppBuilder' introuvable`) : le fork aurait renommé le namespace. Alors dans `src/Piscine.Desktop/Program.cs` remplacer `using Photino.Blazor;` par `using PhotinoX.Blazor;` (et idem dans tout fichier l'utilisant), puis re-builder. *(Non attendu : la doc du fork conserve `namespace Photino.Blazor`.)*

- [ ] **Step 3 : Commit**

```bash
git add src/Piscine.Desktop/Piscine.Desktop.csproj src/Piscine.Desktop/Program.cs
git commit -m "$(printf 'feat(desktop): migrer vers PhotinoX.Blazor 4.2.0 (retrait epingle WebView)\n\nSwap Photino.Blazor 3.2.0 -> PhotinoX.Blazor 4.2.0 (fork maintenu, net10-natif).\nLe fork aligne Microsoft.AspNetCore.Components.WebView sur 10.0.x : l epingle\nmanuelle 10.0.8 + contournement NU1605 deviennent inutiles. API/namespace\nPhotino.Blazor conserves -> aucun changement de code applicatif.\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 2 : Solution complète buildée + suite de tests verte (gate)

**Files:** aucun (porte de vérification).

- [ ] **Step 1 : Build de la solution**

Run : `dotnet build Piscine.slnx -c Release`
Expected : **Build succeeded**, 0 erreur.

- [ ] **Step 2 : Tests**

Run : `dotnet test Piscine.slnx -c Release --no-build`
Expected : **0 échec** (≈267 tests : Core 46 + App 57 + Components 25 + Git 12 + DevHost.E2E 9 + Grading 118).

**Contingence** : si la E2E DevHost (Playwright) échoue par absence de navigateur sur cette machine, relancer en filtrant : `dotnet test Piscine.slnx -c Release --no-build --filter "FullyQualifiedName!~DevHost.E2E"` et noter que l'E2E sera couverte par la CI. Aucune autre catégorie ne doit échouer.

*(Pas de commit : aucune modification de fichier.)*

---

### Task 3 : Répétition locale du packaging — libs natives au bon nom à la racine

**Files:** aucun (porte de vérification ; artefacts sous `artifacts/`, ignorés par git).

- [ ] **Step 1 : Publish desktop win-x64 self-contained**

Run :
```powershell
dotnet publish src/Piscine.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/pkg/piscine-win-x64/desktop
```

- [ ] **Step 2 : Asserter les libs natives Windows à la racine (nouveaux noms)**

Run :
```powershell
Test-Path artifacts/pkg/piscine-win-x64/desktop/PhotinoX.Native.dll ; Test-Path artifacts/pkg/piscine-win-x64/desktop/WebView2Loader.dll
```
Expected : `True` puis `True`. *(Si `False` : la lib n'a pas été aplatie → vérifier que `PublishSingleFile=false` et `--self-contained true` sont bien passés.)*

- [ ] **Step 3 : Publish desktop linux-x64 (cross-publish) + assertion `.so`**

Run :
```powershell
dotnet publish src/Piscine.Desktop -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/pkg/piscine-linux-x64/desktop
Test-Path artifacts/pkg/piscine-linux-x64/desktop/PhotinoX.Native.so
```
Expected : `True`.

*(Pas de commit.)*

---

### Task 4 : Lancer la vraie fenêtre PhotinoX (Windows)

**Files:** aucun (preuve runtime sur cette machine).

- [ ] **Step 1 : Lancer l'app**

Run (en arrière-plan, fenêtre native) :
```powershell
$env:PISCINE_CONTENT = "$PWD\content"; dotnet run --project src/Piscine.Desktop -c Release
```
Expected : une **fenêtre native « Piscine .NET »** s'ouvre et affiche l'accueil (routage des pages RCL). Aucune exception fatale au démarrage (`app.MainWindow.ShowMessage` n'apparaît pas).

- [ ] **Step 2 : Confirmer puis fermer**

Capturer (screenshot) ou confirmer visuellement l'ouverture, puis fermer la fenêtre (le process `dotnet run` se termine). Si exécution headless impossible, consigner la limitation et s'appuyer sur les dry-runs CI.

*(Pas de commit.)*

---

### Task 5 : CI — assertions libs natives + webkit 4.1 (`.github/workflows/ci.yml`)

**Files:**
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1 : Renommer les libs natives dans le dry-run packaging (job `build-test`)**

Remplacer :
```yaml
              win-x64)   test -f "$out/Photino.Native.dll" && test -f "$out/WebView2Loader.dll" ;;
              linux-x64) test -f "$out/Photino.Native.so" ;;
```
par :
```yaml
              win-x64)   test -f "$out/PhotinoX.Native.dll" && test -f "$out/WebView2Loader.dll" ;;
              linux-x64) test -f "$out/PhotinoX.Native.so" ;;
```

Et le commentaire juste au-dessus du step :
```yaml
      # sont posees a la RACINE de sortie (pas runtimes/<rid>/native/) — verifie en T1.
```
→
```yaml
      # PhotinoX sont posees a la RACINE de sortie (pas runtimes/<rid>/native/) — verifie en T1.
```

- [ ] **Step 2 : Passer le job `appimage-offline-dryrun` à webkit 4.1 / ubuntu-24.04**

Remplacer :
```yaml
    # l'empaquetage hors-ligne avant tout tag. ubuntu-22.04 = libwebkit2gtk-4.0 (Photino 3.2.0).
  appimage-offline-dryrun:
    runs-on: ubuntu-22.04
```
par :
```yaml
    # l'empaquetage hors-ligne avant tout tag. ubuntu-24.04 = libwebkit2gtk-4.1 (PhotinoX 4.2.0).
  appimage-offline-dryrun:
    runs-on: ubuntu-24.04
```

Et l'install apt :
```yaml
          sudo apt-get install -y -qq imagemagick libwebkit2gtk-4.0-37 libgtk-3-0 libgtk-3-bin \
```
→
```yaml
          sudo apt-get install -y -qq imagemagick libwebkit2gtk-4.1-0 libgtk-3-0 libgtk-3-bin \
```

- [ ] **Step 3 : Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "$(printf 'ci: assertions libs natives PhotinoX + AppImage dry-run webkit2gtk-4.1\n\nRenomme Photino.Native.{dll,so} -> PhotinoX.Native.{dll,so} dans le dry-run\npackaging ; passe le dry-run AppImage offline sur ubuntu-24.04 + libwebkit2gtk-4.1-0\n(PhotinoX 4.2.0 cible webkit2gtk-4.1).\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 6 : Release — runner Linux + webkit 4.1 (`.github/workflows/release.yml`)

**Files:**
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1 : Job `package-linux` → ubuntu-24.04 + webkit 4.1**

Remplacer :
```yaml
    # ubuntu-22.04 : a libwebkit2gtk-4.0 (Photino 3.2.0) requis pour bundler l'AppImage offline.
  package-linux:
    runs-on: ubuntu-22.04
```
par :
```yaml
    # ubuntu-24.04 : a libwebkit2gtk-4.1 (PhotinoX 4.2.0) requis pour bundler l'AppImage offline.
  package-linux:
    runs-on: ubuntu-24.04
```

Et l'install apt (étape AppImage) :
```yaml
          sudo apt-get install -y -qq imagemagick libwebkit2gtk-4.0-37 libgtk-3-0 libgtk-3-bin \
```
→
```yaml
          sudo apt-get install -y -qq imagemagick libwebkit2gtk-4.1-0 libgtk-3-0 libgtk-3-bin \
```

- [ ] **Step 2 : Commit**

```bash
git add .github/workflows/release.yml
git commit -m "$(printf 'ci(release): package-linux sur ubuntu-24.04 + libwebkit2gtk-4.1 (PhotinoX)\n\nPhotinoX 4.2.0 cible webkit2gtk-4.1 (soup3) ; le runner Linux passe de\nubuntu-22.04 (4.0) a ubuntu-24.04 (4.1) pour bundler l AppImage offline.\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 7 : Script AppImage + AppRun — noms & messages (`build/installer/linux/`)

**Files:**
- Modify: `build/installer/linux/build-appimage.sh`
- Modify: `build/installer/linux/AppRun`

*(Les mécanismes de détection sont déjà version-agnostiques `4.[01]` ; seuls les commentaires/messages 4.0 et le nom de lib native sont à corriger.)*

- [ ] **Step 1 : `build-appimage.sh` — commentaire dépendance ELF (ligne ~47)**

Remplacer :
```bash
  # Photino.Native.so a une dependance ELF DIRECTE vers libwebkit2gtk (4.0 pour Photino.Blazor 3.2.0) :
  # linuxdeploy la bundle AUTOMATIQUEMENT si le webkit correspondant est installe sur la machine de build
  # (Ubuntu 22.04 a libwebkit2gtk-4.0-37 ; 24.04 ne l'a plus). On detecte la version presente.
```
par :
```bash
  # PhotinoX.Native.so charge libwebkit2gtk-4.1 (PhotinoX 4.2.0) : linuxdeploy + le copy des helpers
  # bundlent le webkit installe sur la machine de build (Ubuntu 24.04 a libwebkit2gtk-4.1-0).
  # On detecte la version presente (regex 4.[01], retro-compatible).
```

- [ ] **Step 2 : `build-appimage.sh` — message d'erreur (ligne ~51)**

Remplacer :
```bash
  [ -n "$WK" ] || { echo "ERREUR: libwebkit2gtk introuvable (build env). Sur Ubuntu 22.04: apt install libwebkit2gtk-4.0-37"; exit 2; }
```
par :
```bash
  [ -n "$WK" ] || { echo "ERREUR: libwebkit2gtk introuvable (build env). Sur Ubuntu 24.04: apt install libwebkit2gtk-4.1-0"; exit 2; }
```

- [ ] **Step 3 : `build-appimage.sh` — echo mode online (ligne ~72)**

Remplacer :
```bash
  echo ">> mode online : pas de bundling (webkit2gtk-4.0 + gtk système requis sur le poste)"
```
par :
```bash
  echo ">> mode online : pas de bundling (webkit2gtk-4.1 + gtk système requis sur le poste)"
```

- [ ] **Step 4 : `AppRun` — commentaire (ligne ~13)**

Remplacer :
```sh
# Version-agnostique : 4.0 (Photino 3.2.0) ou 4.1.
```
par :
```sh
# Version-agnostique : 4.1 (PhotinoX 4.2.0) — la boucle accepte aussi 4.0 (retro-compat).
```

- [ ] **Step 5 : Commit**

```bash
git add build/installer/linux/build-appimage.sh build/installer/linux/AppRun
git commit -m "$(printf 'build(appimage): commentaires/messages webkit2gtk-4.1 + PhotinoX.Native\n\nMecanismes deja version-agnostiques (regex 4.[01]) ; alignement des\ncommentaires et du message d erreur sur PhotinoX 4.2.0 / Ubuntu 24.04 / 4.1.\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 8 : Docs recrue/mainteneur (`docs/deploiement.md`, `docs/mise-en-oeuvre.md`, wiki)

**Files:**
- Modify: `docs/deploiement.md`
- Modify: `docs/mise-en-oeuvre.md`
- Modify: `docs/wiki/Mise-en-oeuvre.md`
- Modify: `docs/wiki/Home.md`

- [ ] **Step 1 : `docs/deploiement.md` — section `package-linux` (lignes ~82-98)**

- `### \`package-linux\` (ubuntu-22.04)` → `### \`package-linux\` (ubuntu-24.04)`
- `> Ubuntu **22.04** car Photino.Blazor 3.2.0 cible **webkit2gtk-4.0** (la 24.04 ne fournit plus la 4.0).` → `> Ubuntu **24.04** car PhotinoX.Blazor 4.2.0 cible **webkit2gtk-4.1** (soup3) ; la 22.04 ne fournit que la 4.0.`
- `   - Les **libs natives Photino** sont à la **racine** du dossier \`desktop/\` (\`Photino.Native.dll\` +` → `   - Les **libs natives PhotinoX** sont à la **racine** du dossier \`desktop/\` (\`PhotinoX.Native.dll\` +`
- `     \`WebView2Loader.dll\` Windows, \`Photino.Native.so\` Linux) — **pas** sous \`runtimes/<rid>/native/\`.` → `     \`WebView2Loader.dll\` Windows, \`PhotinoX.Native.so\` Linux) — **pas** sous \`runtimes/<rid>/native/\`.`
- `   - \`piscine-<tag>-linux-x86_64-offline.AppImage\` — **webkit2gtk-4.0 + gtk + git bundlés** → tourne` → `...**webkit2gtk-4.1 + gtk + git bundlés**...`
- `   - \`piscine-<tag>-linux-x86_64-online.AppImage\` — léger, s'appuie sur le \`libwebkit2gtk-4.0\` système.` → `...le \`libwebkit2gtk-4.1\` système.`

- [ ] **Step 2 : `docs/deploiement.md` — prérequis webview + répétition locale + smoke (lignes ~130-180)**

- `- **Linux** : **\`libwebkit2gtk-4.0\`** (Photino 3.2.0 — **pas** 4.1) — Debian/Ubuntu` → `- **Linux** : **\`libwebkit2gtk-4.1\`** (PhotinoX 4.2.0) — Debian/Ubuntu`
- `  \`sudo apt install libwebkit2gtk-4.0-37\`, Fedora \`sudo dnf install webkit2gtk4.0\`.` → `  \`sudo apt install libwebkit2gtk-4.1-0\`, Fedora \`sudo dnf install webkit2gtk4.1\`.`
- `Test-Path artifacts/pkg/piscine-win-x64/desktop/Photino.Native.dll   # → True` → `Test-Path artifacts/pkg/piscine-win-x64/desktop/PhotinoX.Native.dll   # → True`
- `- [ ] **Linux (AppImage offline)** : \`*-offline.AppImage\` sur une machine **sans** \`libwebkit2gtk-4.0\` et` → `...**sans** \`libwebkit2gtk-4.1\` et`
- `- [ ] **Linux (AppImage online)** : \`*-online.AppImage\` (webkit système / \`apt install libwebkit2gtk-4.0-37\`) → même flux.` → `...\`apt install libwebkit2gtk-4.1-0\`...`

- [ ] **Step 3 : `docs/mise-en-oeuvre.md` (lignes ~35-167)**

- `    - **Linux** : **\`libwebkit2gtk-4.0\`** — Debian/Ubuntu \`sudo apt install libwebkit2gtk-4.0-37\`,` → `    - **Linux** : **\`libwebkit2gtk-4.1\`** — Debian/Ubuntu \`sudo apt install libwebkit2gtk-4.1-0\`,`
- `      Fedora \`sudo dnf install webkit2gtk4.0\`. *(Photino 3.2.0 cible la série 4.0, pas 4.1.)*` → `      Fedora \`sudo dnf install webkit2gtk4.1\`. *(PhotinoX 4.2.0 cible la série 4.1.)*`
- `  - L'**AppImage online** s'appuie sur le \`libwebkit2gtk-4.0\` du système (§1).` → `...le \`libwebkit2gtk-4.1\` du système (§1).`
- `  l'**installeur** (qui le gère), ou installer le runtime à la main (WebView2 / \`libwebkit2gtk-4.0\`).` → `...(WebView2 / \`libwebkit2gtk-4.1\`).`

- [ ] **Step 4 : Wiki (`docs/wiki/Mise-en-oeuvre.md` ligne ~11, `docs/wiki/Home.md` ligne ~22)**

Dans les deux : `libwebkit2gtk-4.0` → `libwebkit2gtk-4.1`.

- [ ] **Step 5 : Commit**

```bash
git add docs/deploiement.md docs/mise-en-oeuvre.md docs/wiki/Mise-en-oeuvre.md docs/wiki/Home.md
git commit -m "$(printf 'docs: PhotinoX 4.2.0 + webkit2gtk-4.1 (deploiement, mise-en-oeuvre, wiki)\n\nNoms libs natives Photino.Native -> PhotinoX.Native, prereq webview Linux\n4.0 -> 4.1, runner package-linux ubuntu-24.04.\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 9 : ADR du choix de fork + entrée CHANGELOG v3.1.0

**Files:**
- Create: `docs/superpowers/adr/2026-06-08-photinox-fork.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1 : Écrire l'ADR**

Créer `docs/superpowers/adr/2026-06-08-photinox-fork.md` :
```markdown
# ADR — Migration vers le fork PhotinoX.Blazor 4.2.0

> Décision : 2026-06-08. Statut : **ACTÉE**. Portée : hôte de bureau `Piscine.Desktop` uniquement.

## Contexte

`Piscine.Desktop` reposait sur `Photino.Blazor 3.2.0` (Photino.NET v3), qui :
- tire `Microsoft.AspNetCore.Components.WebView` en **8.0.x** → épingle manuelle `10.0.8` pour éviter
  un downgrade NU1605 sous `TreatWarningsAsErrors` ;
- cible **webkit2gtk-4.0** (soup2) sous Linux → runner figé `ubuntu-22.04`, prérequis `libwebkit2gtk-4.0`.

## Décision

Migrer vers **`PhotinoX.Blazor 4.2.0`** (fork maintenu d'`ivanvoyager`, Apache-2.0,
<https://github.com/ivanvoyager/PhotinoX.Blazor>), qui **cible nativement net8/9/10** et aligne WebView
sur **10.0.x**.

## Conséquences

- **+** Suppression de l'épingle WebView et du contournement NU1605.
- **+** Dépendance net10-native, activement maintenue (vs Photino 3.x).
- **~** Linux passe à **webkit2gtk-4.1** (soup3) : runner `ubuntu-24.04`, prérequis zip `libwebkit2gtk-4.1`,
  bundling AppImage offline en 4.1. Aligné sur les distros récentes (la 4.0 est en fin de vie upstream).
- **~** Libs natives renommées : `PhotinoX.Native.{dll,so}` (Windows garde `WebView2Loader.dll`) →
  assertions CI et docs mises à jour.
- **=** **Aucun** changement d'API : namespace `Photino.Blazor` et `PhotinoBlazorAppBuilder` conservés
  par le fork → code applicatif (`Program.cs`, pages, composants) inchangé.

## Alternative écartée

`Photino.Blazor 4.0.13` (upstream) : également net-multi-cible, mais PhotinoX est en avance de version
(4.2.0) et net10-natif ; le choix du fork est assumé pour suivre la maintenance active.
```

- [ ] **Step 2 : Prépendre l'entrée CHANGELOG v3.1.0** (juste sous l'en-tête, avant `## [v3.0.0]`)

Insérer :
```markdown
## [v3.1.0] — 2026-06-08

Migration d'infrastructure de l'app de bureau : passage du paquet **`Photino.Blazor 3.2.0`** au fork
maintenu **`PhotinoX.Blazor 4.2.0`** (net10-natif). **Transparent pour la recrue** (même UX, mêmes
pages) ; le moteur de notation, le **CLI headless `piscine`** et `grade-received` restent **compatibles
`v2.0.0`**.

### Changé

- **`Piscine.Desktop`** : `Photino.Blazor 3.2.0` → **`PhotinoX.Blazor 4.2.0`**. Le fork aligne
  `Microsoft.AspNetCore.Components.WebView` sur **10.0.x** → l'**épingle manuelle `10.0.8`** et le
  contournement **NU1605** sont supprimés. API (`PhotinoBlazorAppBuilder`, namespace `Photino.Blazor`)
  inchangée → **aucun changement de code applicatif**.
- **Libs natives** renommées dans le paquet : `PhotinoX.Native.dll` (Windows) / `PhotinoX.Native.so`
  (Linux), `WebView2Loader.dll` conservé. Toujours à la **racine** de `desktop/`.
- **Linux — webkit2gtk-4.0 → 4.1** : le job `package-linux` (et le dry-run AppImage CI) passent à
  **`ubuntu-24.04`** + **`libwebkit2gtk-4.1-0`** ; l'AppImage **offline** bundle désormais webkit 4.1.
  **Prérequis du zip portable Linux** : `libwebkit2gtk-4.1` (au lieu de `4.0`). Les **installeurs**
  (AppImage offline / online) gèrent ce prérequis comme avant.

### Notes

- ADR : [docs/superpowers/adr/2026-06-08-photinox-fork.md](docs/superpowers/adr/2026-06-08-photinox-fork.md).
- Windows : runtime **WebView2** inchangé (Evergreen, géré par les installeurs).
```

- [ ] **Step 3 : Commit**

```bash
git add docs/superpowers/adr/2026-06-08-photinox-fork.md CHANGELOG.md
git commit -m "$(printf 'docs(changelog): entree v3.1.0 (migration PhotinoX) + ADR fork\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 10 : Mise à jour `HANDOFF.md`

**Files:**
- Modify: `docs/superpowers/HANDOFF.md`

- [ ] **Step 1 : Mettre à jour la date d'en-tête** : `Dernière MAJ : 2026-06-07.` → `Dernière MAJ : 2026-06-08.`

- [ ] **Step 2 : Ajouter une puce en tête d'« État actuel »** (juste avant la puce `🏷️ Release v3.0.0`) :
```markdown
- **🔧 Migration PhotinoX (sprint 2026-06-08, branche `feat/photinox-migration`)** : `Piscine.Desktop`
  passe de `Photino.Blazor 3.2.0` à **`PhotinoX.Blazor 4.2.0`** (fork net10-natif). Épingle WebView
  NU1605 **supprimée** ; libs natives **`PhotinoX.Native.{dll,so}`** ; Linux **webkit2gtk-4.1**
  (runner `ubuntu-24.04`). API/namespace `Photino.Blazor` conservés → **0 changement de code applicatif**.
  Spec : `specs/2026-06-08-photinox-migration-design.md` ; ADR : `adr/2026-06-08-photinox-fork.md`.
  Cible release **v3.1.0**.
```

- [ ] **Step 3 : Corriger les références 4.0 / Photino devenues fausses** (rester factuel sur l'historique v3.0.0, ne corriger que ce qui décrit l'état *courant*) :
  - ligne ~335 / ~389 : `Photino.Native.dll/.so/.dylib` → `PhotinoX.Native.dll/.so/.dylib`.
  - ligne ~223 : le « CONSTAT clé : Photino.Blazor 3.2.0 → webkit2gtk-4.0 » est un constat historique de v3.0.0 → ajouter en fin de ligne : ` (v3.1.0 : PhotinoX 4.2.0 → webkit2gtk-4.1).`
  - ligne ~217 : `bâti sur ubuntu-22.04` → `bâti sur ubuntu-24.04 (v3.1.0)`.

*(Les autres mentions 4.0 décrivent ce que v3.0.0 a livré et restent exactes — ne pas les réécrire.)*

- [ ] **Step 4 : Commit**

```bash
git add docs/superpowers/HANDOFF.md
git commit -m "$(printf 'docs(handoff): consigner la migration PhotinoX (v3.1.0 cible)\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

### Task 11 : Pousser la branche, ouvrir la PR, attendre la CI verte

**Files:** aucun.

- [ ] **Step 1 : Pousser**

```bash
git push -u origin feat/photinox-migration
```

- [ ] **Step 2 : Ouvrir la PR**

```bash
gh pr create --base main --head feat/photinox-migration \
  --title "Migration PhotinoX.Blazor 4.2.0 (v3.1.0)" \
  --body "$(printf 'Migre Piscine.Desktop de Photino.Blazor 3.2.0 vers le fork maintenu PhotinoX.Blazor 4.2.0 (net10-natif).\n\n## Changements\n- csproj : swap paquet + retrait epingle WebView NU1605 (aucun code applicatif change ; namespace Photino.Blazor conserve).\n- Libs natives : Photino.Native.{dll,so} -> PhotinoX.Native.{dll,so} (assertions CI + docs).\n- Linux : webkit2gtk-4.0 -> 4.1 (package-linux + dry-run AppImage sur ubuntu-24.04 ; AppImage offline bundle 4.1).\n- Docs : deploiement, mise-en-oeuvre, wiki, CHANGELOG v3.1.0, ADR, HANDOFF.\n\n## Verification locale\n- build solution + ~267 tests verts.\n- publish desktop win-x64/linux-x64 : libs natives PhotinoX a la racine de desktop/.\n- fenetre native PhotinoX lancee sur Windows.\n\nLa CI exerce les dry-runs packaging (win+linux), AppImage offline lance hors-ligne (ubuntu-24.04/webkit 4.1), installeur Inno.\n\n🤖 Generated with [Claude Code](https://claude.com/claude-code)')"
```

- [ ] **Step 3 : Attendre la CI**

```bash
gh pr checks --watch
```
Expected : tous les jobs **verts** (`build-test`, `appimage-offline-dryrun`, `windows-installer-dryrun`).

**Contingence** : si `appimage-offline-dryrun` échoue (webkit 4.1 introuvable, helper manquant, ou app qui ne charge pas `app://localhost`), lire le log du step « Vérifier le lancement HORS-LIGNE », corriger `build-appimage.sh`/apt deps, committer sur la branche, re-pousser. Ne pas taguer tant que la CI n'est pas verte.

---

### Task 12 : Merger, taguer v3.1.0, publier la release, vérifier les artefacts

**Files:** aucun (action publique — la migration doit être verte sur `main`).

- [ ] **Step 1 : Merger la PR (squash, convention du repo)**

```bash
gh pr merge feat/photinox-migration --squash --delete-branch
```

- [ ] **Step 2 : Se mettre à jour sur `main` et attendre la CI verte sur `main`**

```bash
git checkout main && git pull origin main
gh run list --branch main --limit 1 --json status,conclusion
```
Expected : dernier run `completed` / `success`.

- [ ] **Step 3 : Taguer et pousser le tag (déclenche `release.yml`)**

```bash
git tag v3.1.0
git push origin v3.1.0
```

- [ ] **Step 4 : Suivre la release**

```bash
gh run list --workflow release.yml --limit 1 --json status,conclusion,headBranch
gh run watch $(gh run list --workflow release.yml --limit 1 --json databaseId -q '.[0].databaseId')
```
Expected : `release.yml` **vert** (3 jobs : `package-linux`, `package-windows`, `release`).

- [ ] **Step 5 : Vérifier les artefacts attachés**

```bash
gh release view v3.1.0
```
Expected : **6 artefacts** — `piscine-v3.1.0-win-x64.zip`, `piscine-v3.1.0-linux-x64.zip`,
`piscine-v3.1.0-linux-x86_64-offline.AppImage`, `piscine-v3.1.0-linux-x86_64-online.AppImage`,
`piscine-v3.1.0-win-x64-offline-setup.exe`, `piscine-v3.1.0-win-x64-online-setup.exe`.

**Contingence** (release ratée) : `gh release delete v3.1.0 --yes` ; `git push origin :refs/tags/v3.1.0` ;
`git tag -d v3.1.0` ; corriger sur `main` ; re-taguer (cf. deploiement.md §6).

- [ ] **Step 6 : Notes de release depuis le CHANGELOG**

Extraire la section `[v3.1.0]` du CHANGELOG vers `release-notes.md`, puis :
```bash
gh release edit v3.1.0 --notes-file release-notes.md
```

---

### Task 13 : Retex du sprint

**Files:**
- Create: `docs/superpowers/retex/2026-06-08-photinox-migration.md`

- [ ] **Step 1 : Écrire le retex** (sur `main`, après release) — ce qui a marché (drop-in API, libs au bon nom, CI verte), surprises (rename `PhotinoX.Native.*`, scripts AppImage déjà 4.1-ready), et le résultat (release v3.1.0, N artefacts, X tests verts).

- [ ] **Step 2 : Commit & push**

```bash
git add docs/superpowers/retex/2026-06-08-photinox-migration.md
git commit -m "$(printf 'docs(retex): migration PhotinoX v3.1.0\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
git push origin main
```

---

## Auto-revue du plan (writing-plans)

- **Couverture du spec** : §4a→T1 ; §4b→T6 ; §4c→T5 ; §4d→T7 ; §4e (piscine.iss)→ *aucune réf. de nom de lib native dans `piscine.iss`* (le dry-run Windows utilise un payload stub `Piscine.Desktop.exe` ; l'.iss embarque le dossier `desktop/` entier sans nommer les libs) → **pas de modif requise**, vérifié au grep ; §4f→T8/T9/T10/T13 ; §5 (noms natifs)→résolus en amont (nupkg) et asserts en T3/T5 ; §6→T2/T3/T4 + CI ; §7→T12.
- **Placeholders** : aucun `TBD`/`TODO` ; tous les old→new strings sont littéraux.
- **Cohérence des noms** : `PhotinoX.Native.dll`/`PhotinoX.Native.so`/`WebView2Loader.dll`, `libwebkit2gtk-4.1-0`, `ubuntu-24.04`, tag `v3.1.0` — homogènes dans tout le plan.
