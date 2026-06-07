# Déploiement — préparer et publier une release

Ce guide s'adresse au **mainteneur** qui publie une nouvelle version de la piscine. La recrue, elle,
consomme le résultat (le zip self-contained) via [docs/mise-en-oeuvre.md](mise-en-oeuvre.md).

Publier = **pousser un tag `v*`**. Le workflow [`.github/workflows/release.yml`](../.github/workflows/release.yml)
fait tout le reste : il assemble le contenu, publie un binaire self-contained par OS, l'empaquette
et crée la **GitHub Release** avec les zips attachés.

> ⚠️ **Action publique et quasi irréversible.** Pousser un tag `v*` déclenche immédiatement la
> publication d'une release visible. Ne tague qu'après la check-list ci-dessous et avec l'accord du
> propriétaire du dépôt.

---

## 1. Versionnement

- **SemVer** : `vMAJEUR.MINEUR.CORRECTIF` (ex. `v2.0.0`).
- **Le tag git est l'unique source de vérité.** Il n'y a **aucun numéro de version** dans les
  `.csproj` ni dans `Directory.Build.props` : le workflow nomme la release et les zips d'après
  `github.ref_name` (le nom du tag). Pas de fichier à bumper avant de taguer.
- **Quand incrémenter MAJEUR** : ajout d'un palier de contenu / d'un grader, changement de format de
  contenu ou de CLI susceptible de casser un workspace existant.
- Historique des versions : [CHANGELOG.md](../CHANGELOG.md).

> Note de nommage : la *roadmap* parle de « paliers » v1 / v2 / v3 (cf.
> [roadmap-v2-v3](superpowers/plans/2026-05-31-roadmap-v2-v3.md)). Ce sont des **phases de contenu**,
> pas des numéros de release. Exemple : la release **`v2.0.0`** embarque à la fois le palier de
> contenu v2 (M24–M35) **et** le palier v3 (M36–M39). Documenter ce mapping dans le CHANGELOG.

---

## 2. Check-list avant tag

Tout doit être vert **sur `main`**, arbre propre :

- [ ] `dotnet test Piscine.slnx -c Release` → **0 échec** (mettre à jour le compteur dans le HANDOFF).
- [ ] `validate-content` → **« Contenu valide. »** :
      ```powershell
      $env:PISCINE_CONTENT = "$PWD\content"; dotnet run --project src/Piscine.Cli -c Release -- validate-content
      ```
- [ ] `docs/wiki/Curriculum.md` reflète les modules/Rushes livrés.
- [ ] Entrée ajoutée en tête de [CHANGELOG.md](../CHANGELOG.md) pour la version visée.
- [ ] `git status` propre, tout poussé, **CI verte sur `main`** :
      ```powershell
      gh run list --branch main --limit 1 --json status,conclusion
      ```
- [ ] (Recommandé) **répétition locale du packaging** — voir §5 — pour détecter une régression
      d'assemblage sans déclencher de release.

---

## 3. Publier

Faire les appels **séparément** (un refus de push ne doit pas perdre le commit) :

```powershell
# 1. La doc de release (CHANGELOG, HANDOFF, Curriculum) est commitée et poussée sur main
git push origin main
# 2. Attendre la CI verte sur le commit à taguer (voir check-list)
# 3. Taguer ce commit, puis pousser le tag (déclenche release.yml)
git tag v2.0.0
git push origin v2.0.0
```

Suivre l'exécution :

```powershell
gh run list --workflow release.yml --limit 1 --json status,conclusion,headBranch
gh release view v2.0.0
```

---

## 4. Ce que produit `release.yml`

> **Plateformes : Windows + Linux.** macOS (`osx-arm64`) est **abandonné** depuis v5 (pas de runner
> macOS pour prouver la fenêtre native ; webview WKWebView non automatisable en CI).

Le workflow a **3 jobs** :

### `package-linux` (ubuntu-22.04)

> Ubuntu **22.04** car Photino.Blazor 3.2.0 cible **webkit2gtk-4.0** (la 24.04 ne fournit plus la 4.0).

1. **`package-content content artifacts/content`** — copie le contenu **sans les `solution/`**
   (les corrigés ne sont jamais distribués).
2. **Zips self-contained `win-x64` + `linux-x64`** (cross-publiés depuis Linux) : CLI `piscine` (runtime
   .NET + Roslyn embarqués — c'est le binaire que le hook `post-receive` appelle), app de bureau
   `Piscine.Desktop` dans `desktop/`, **shim git** `Piscine.GitShim` dans `desktop/gitshim/`, et le
   `content/` assemblé. Lanceurs `start-piscine-desktop.{cmd,sh}` ; **Windows uniquement** : MinGit
   portable dans `mingit/` + `start-piscine.cmd`. Zips nommés `piscine-<tag>-<rid>.zip`.
   - Les **libs natives Photino** sont à la **racine** du dossier `desktop/` (`Photino.Native.dll` +
     `WebView2Loader.dll` Windows, `Photino.Native.so` Linux) — **pas** sous `runtimes/<rid>/native/`.
3. **AppImage Linux** en deux variantes (`linuxdeploy` + plugin GTK) :
   - `piscine-<tag>-linux-x86_64-offline.AppImage` — **webkit2gtk-4.0 + gtk + git bundlés** → tourne
     **hors-ligne**, sans rien installer sur le poste.
   - `piscine-<tag>-linux-x86_64-online.AppImage` — léger, s'appuie sur le `libwebkit2gtk-4.0` système.

### `package-windows` (windows-latest)

Installe Inno Setup (choco), assemble le **payload** (CLI + desktop + gitshim + content + MinGit), puis
compile **deux installeurs** Inno (`ISCC /DMODE`) — installation **par utilisateur** (`PrivilegesRequired=lowest`),
raccourcis menu Démarrer + Bureau :

- `piscine-<tag>-win-x64-offline-setup.exe` — runtime **WebView2 Standalone Evergreen** embarqué (full,
  hors-ligne), installé **si WebView2 manque**.
- `piscine-<tag>-win-x64-online-setup.exe` — **bootstrapper** WebView2 (léger, télécharge le runtime
  manquant).

> Fwlinks WebView2 (stables) : offline `go.microsoft.com/fwlink/?linkid=2124701`
> (`MicrosoftEdgeWebView2RuntimeInstallerX64.exe`), online `.../p/?LinkId=2124703`
> (`MicrosoftEdgeWebview2Setup.exe`).

### `release` (ubuntu-latest)

Agrège les artefacts des deux jobs et crée la **GitHub Release** :
`gh release create <tag>` attache les **zips** (win + linux), les **AppImages** (offline + online) et
les **installeurs Windows** (offline + online). Titre `Piscine .NET <tag>`.

### Prérequis webview (par OS)

L'app de bureau rend son interface dans le **webview système**. Avec un **installeur**, le runtime est
**géré** (offline = embarqué/installé au besoin ; online = téléchargé). En **mode zip** (portable), il
peut falloir l'installer à la main :

- **Windows** : runtime **WebView2** (Evergreen). Préinstallé sur Windows 11 et les Windows 10
  récents. **Éditions N** / images minimales : installer l'[Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
  (`WebView2Loader.dll` est dans le paquet ; c'est le **runtime** qui peut manquer, pas le loader.)
- **Linux** : **`libwebkit2gtk-4.0`** (Photino 3.2.0 — **pas** 4.1) — Debian/Ubuntu
  `sudo apt install libwebkit2gtk-4.0-37`, Fedora `sudo dnf install webkit2gtk4.0`.
  *(L'AppImage **offline** l'embarque → aucun prérequis.)*

### Notes de release

`release.yml` pose une **note générique** (résumé des artefacts). Pour publier de vraies notes
(recommandé), les remplacer après coup à partir du CHANGELOG :

```powershell
gh release edit v2.0.0 --notes-file release-notes.md   # extrait de la section visée du CHANGELOG
```

---

## 5. Répétition locale (sans taguer)

Reproduire l'assemblage pour vérifier avant de déclencher la vraie release :

```powershell
dotnet run --project src/Piscine.Cli -c Release -- package-content content artifacts/content
dotnet publish src/Piscine.Cli -c Release -r win-x64 --self-contained true -o artifacts/pkg/piscine-win-x64
# vérifier que artifacts/content ne contient AUCUN dossier solution/

# App de bureau (même opération que la release) + assertion des libs natives à la racine :
dotnet publish src/Piscine.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o artifacts/pkg/piscine-win-x64/desktop
Test-Path artifacts/pkg/piscine-win-x64/desktop/Photino.Native.dll   # → True
Test-Path artifacts/pkg/piscine-win-x64/desktop/WebView2Loader.dll   # → True
```

> La **CI** (`ci.yml`) exécute déjà des *dry-runs* de packaging à chaque PR — une régression
> d'empaquetage est attrapée **avant** tout tag, sans déclencher de release :
> - **publish desktop self-contained** (win-x64 + linux-x64) + assertion des **libs natives à la racine** ;
> - **AppImage offline** bâti puis **lancé hors-ligne** (docker `--network=none`, conteneur sans webkit + xvfb) ;
> - **installeur Windows** compilé par Inno (`ISCC`, offline + online) — garde de compilation.

### Smoke pré-release par OS (action propriétaire)

Les *dry-runs* CI prouvent que les paquets se **construisent** (et, pour l'AppImage offline, qu'il
**démarre hors-ligne**) ; ils ne peuvent pas valider une fenêtre native visuellement. Pour ça,
**taguer une pré-release** (`gh release ... --prerelease`), récupérer chaque artefact et dérouler :

- [ ] **Windows (installeur offline)** : `piscine-<v>-win-x64-offline-setup.exe` sur une machine
      **sans internet** → installe (sans admin) → raccourci « Piscine .NET » → la fenêtre **route le flux** :
      Accueil → cours (titre + bloc de code **colorisé**) → *Vérifier* (sélection d'un exo → verdict +
      diff/indice) → *Progression* → *Initialiser* → *Terminal* (`git init` puis `git commit` rien stagé
      → **carte de coaching**) → *Résultat* (riche après un `git push`).
- [ ] **Windows (installeur online)** : idem sur une machine **sans WebView2** → l'installeur télécharge le runtime.
- [ ] **Linux (AppImage offline)** : `*-offline.AppImage` sur une machine **sans** `libwebkit2gtk-4.0` et
      **hors-ligne** → même flux.
- [ ] **Linux (AppImage online)** : `*-online.AppImage` (webkit système / `apt install libwebkit2gtk-4.0-37`) → même flux.
- [ ] **Terminal + coaching** confirmés dans la fenêtre native (page *Terminal*) sur Windows et Linux.
- [ ] **CLI intact** (zips) : `piscine init` puis `piscine status` répondent (le hook de correction
      n'est pas affecté par l'app de bureau ni par les installeurs).

---

## 6. En cas de problème

- **`release.yml` échoue** : corriger sur `main`, **supprimer** la release ratée et son tag, puis
  re-taguer :
  ```powershell
  gh release delete v2.0.0 --yes
  git push origin :refs/tags/v2.0.0   # supprime le tag distant
  git tag -d v2.0.0                    # supprime le tag local
  ```
- **Mauvais contenu publié** : un corrigé qui apparaît dans un zip = `package-content` mal appelé ;
  vérifier l'étape §4.1 et la répétition locale §5.
- **SmartScreen / antivirus** côté recrue : c'est attendu (binaire non signé) — documenté dans
  [docs/mise-en-oeuvre.md](mise-en-oeuvre.md#5-dépannage).
