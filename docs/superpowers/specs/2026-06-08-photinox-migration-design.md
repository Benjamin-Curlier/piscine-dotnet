# Spec — Migration `Photino.Blazor 3.2.0` → `PhotinoX.Blazor 4.2.0`

> Design validé en brainstorming le 2026-06-08. Cible : migrer l'hôte de bureau `Piscine.Desktop`
> du paquet **`Photino.Blazor 3.2.0`** vers le fork maintenu **`PhotinoX.Blazor 4.2.0`**, sans
> changer ni le moteur, ni le rendu git, ni l'UX recrue.
> À lire avec [HANDOFF](../HANDOFF.md), la spec [v4 Photino Desktop](2026-06-06-v4-photino-desktop-design.md)
> et [docs/deploiement.md](../../deploiement.md).
>
> **⚠️ Écart constaté à l'implémentation (2026-06-08)** : l'**AppImage offline** (webkit bundlé) est
> **abandonnée** en v3.1.0 — WebKitGTK en build release ignore `WEBKIT_EXEC_PATH` et résout ses process
> auxiliaires via un chemin absolu compilé, rendant impossible un webkit embarqué fonctionnel sans
> bind-mount privilégié. Seule l'**AppImage online** est publiée. Détail : [ADR](../adr/2026-06-08-photinox-fork.md)
> et [retex](../retex/2026-06-08-photinox-migration.md). Les §4b/§7 ci-dessous (qui mentionnent l'offline)
> reflètent l'intention de départ, pas le résultat livré.

## 1. Contexte & motivation

`Piscine.Desktop` (l'app de bureau Blazor hybride livrée depuis v3.0.0) repose sur
**`Photino.Blazor 3.2.0`** (sur Photino.NET v3). Conséquences actuelles, toutes assumées comme
des contournements dans le code et la CI :

- Le csproj **épingle manuellement** `Microsoft.AspNetCore.Components.WebView` en `10.0.8` pour
  éviter un downgrade NU1605 (Photino.Blazor 3.x tire WebView **8.0.x**, incompatible avec
  `TreatWarningsAsErrors`).
- Linux est figé sur **`webkit2gtk-4.0`** (soup2) : le job `package-linux` tourne sur
  **`ubuntu-22.04`** (la 24.04 ne fournit plus le paquet `-dev` 4.0), l'AppImage **offline** bundle
  `libwebkit2gtk-4.0-37`, et les docs prescrivent `libwebkit2gtk-4.0` aux utilisateurs du zip.

**`PhotinoX.Blazor 4.2.0`** est un *fork maintenu* de Photino.Blazor (auteur `ivanvoyager`,
Apache-2.0, dépôt <https://github.com/ivanvoyager/PhotinoX.Blazor>) qui **cible nativement net8/9/10**
et aligne `Microsoft.AspNetCore.Components.WebView` sur **10.0.x**. Migrer permet de :

- **supprimer** l'épingle WebView manuelle et son commentaire NU1605 (le contournement n'a plus lieu d'être) ;
- suivre une dépendance **activement maintenue** et **net10-native** (vs Photino 3.x) ;
- s'aligner sur l'écosystème Linux courant : PhotinoX 4.x exige **`webkit2gtk-4.1`** (soup3), ce que
  fournissent les distributions récentes (Ubuntu 24.04, Fedora 40+) alors que la 4.0 est en fin de vie.

**Point d'ancrage non négociable** : `Piscine.Desktop` n'est que l'**hôte** de l'UX recrue. Le moteur
(`Core`/`Grading`/`Git`), le **CLI headless `piscine`** et `grade-received` (hook `post-receive`)
**ne sont pas touchés**. La migration est une affaire de **dépendance + packaging + CI + docs**.

## 2. Objectifs / Non-objectifs

**Objectifs**
- `Piscine.Desktop` build, teste et **s'exécute** sur `PhotinoX.Blazor 4.2.0` (fenêtre native ouverte,
  routage des pages inchangé).
- Le contournement d'épingle WebView est retiré ; `dotnet build` reste **vert sous `TreatWarningsAsErrors`**.
- La chaîne de packaging/release Linux passe à **`webkit2gtk-4.1`** (CI, AppImage, docs) de bout en bout.
- Les assertions de packaging (libs natives présentes à la racine de `desktop/`) reflètent les **vrais
  noms de fichiers** livrés par PhotinoX.
- Release **`v3.1.0`** publiée via le flux standard (PR → merge → tag → `release.yml`), artefacts attachés.

**Non-objectifs**
- Aucun changement du moteur, des graders, du format de contenu, du CLI, ni du rendu git.
- Aucune refonte de l'UX, des pages ou des composants (`Piscine.Components` inchangé).
- Pas de retour de macOS (abandonné depuis v5).
- Pas d'évolution fonctionnelle : c'est une migration d'infrastructure, **transparente pour la recrue**
  (hormis le pré-requis webview Linux du **zip portable** : `4.0` → `4.1`).

## 3. Décisions actées (brainstorming 2026-06-08)

| # | Décision | Conséquence |
|---|---|---|
| D1 | Cible = **`PhotinoX.Blazor 4.2.0`** (fork), pas l'upstream `Photino.Blazor 4.0.13` | Dépendance net10-native, maintenue, en avance de version |
| D2 | Le fork conserve l'API (`namespace Photino.Blazor`, `PhotinoBlazorAppBuilder`, `MainWindow`) | **Aucun changement de code applicatif attendu** (`Program.cs`, `App.razor`, `_Imports`, `index.html`) |
| D3 | **Supprimer** l'épingle `Microsoft.AspNetCore.Components.WebView 10.0.8` + commentaire NU1605 | Contournement caduc ; à reconfirmer par un build vert |
| D4 | Linux : `webkit2gtk-4.0` → **`webkit2gtk-4.1`** | `ubuntu-22.04` → `ubuntu-24.04`, deps apt, bundling AppImage offline, docs |
| D5 | Version de release = **`v3.1.0`** (mineure) | Pas de rupture moteur/CLI/contenu ; le tag reste l'unique source de vérité |
| D6 | Flux = **PR → merge `main` → tag → `release.yml`** | Artefacts complets (zips + AppImages + installeurs) construits et attachés par la CI |

## 4. Périmètre des changements

**a. `src/Piscine.Desktop/Piscine.Desktop.csproj`**
- `Photino.Blazor 3.2.0` → `PhotinoX.Blazor 4.2.0`.
- Retirer la `PackageReference Microsoft.AspNetCore.Components.WebView 10.0.8` et son commentaire
  NU1605 (le fork aligne déjà WebView sur 10.0.x en net10). Si un `PhotinoX.Native` explicite s'avère
  nécessaire pour copier les assets natifs, l'ajouter (à confirmer à l'implémentation).

**b. `.github/workflows/release.yml` — job `package-linux`**
- `runs-on: ubuntu-22.04` → `ubuntu-24.04`.
- apt : `libwebkit2gtk-4.0-37` → `libwebkit2gtk-4.1-0` (+ paquets associés au besoin pour l'AppImage).
- Commentaire « Photino 3.2.0 / webkit2gtk-4.0 » → PhotinoX 4.2.0 / 4.1.

**c. `.github/workflows/ci.yml`**
- Mêmes bumps runner/webkit pour le *dry-run* AppImage et le conteneur de lancement offline
  (`--network=none`).
- Assertions des **libs natives à la racine** de `desktop/` : aligner sur les vrais noms (cf. §5).

**d. `build/installer/linux/build-appimage.sh` (+ `AppRun`, `piscine.desktop` si besoin)**
- Bundler `webkit2gtk-4.1` au lieu de `4.0` dans la variante **offline**.

**e. `build/installer/windows/piscine.iss`**
- Vérifier toute référence à un nom de lib native (ex. `Photino.Native.dll`) et l'aligner (cf. §5).
  Le runtime WebView2 (Windows) est inchangé.

**f. Docs & changelog**
- `README.md`, `docs/deploiement.md`, `docs/mise-en-oeuvre.md` : formulation Photino → PhotinoX,
  pré-requis webview Linux `4.0` → `4.1`, noms de libs natives, runner `ubuntu-24.04`.
- `CHANGELOG.md` : nouvelle entrée en tête **`## [v3.1.0]`**.
- ADR court actant le choix du fork et le passage webkit 4.1.
- Artefacts de sprint : ce spec, le **plan** (`docs/superpowers/plans/`), le **retex**
  (`docs/superpowers/retex/`) et mise à jour de **`HANDOFF.md`**.

**g. Tests**
- `dotnet test Piscine.slnx -c Release` reste vert (≈267 tests). Vérifier qu'aucun test ni script ne
  référence l'ancien nom de paquet/lib.

## 5. Risque ouvert — noms des libs natives (résolu empiriquement)

Le publish self-contained **aplatit** `runtimes/<rid>/native/` vers la racine du dossier de sortie.
Aujourd'hui docs et CI assertent à la racine de `desktop/` : `Photino.Native.dll` + `WebView2Loader.dll`
(Windows), `Photino.Native.so` (Linux). PhotinoX **peut** renommer ces fichiers (`PhotinoX.Native.*`).

**Résolution** : première étape d'implémentation = `dotnet publish src/Piscine.Desktop -r win-x64` et
`-r linux-x64` (self-contained), **inspecter les noms réels** des libs natives produites, **puis**
aligner les assertions CI (`ci.yml`), les références de `piscine.iss`, le bundling AppImage et les docs
sur ces noms. Aucune supposition n'est figée avant cette inspection.

## 6. Vérification (avant tout tag)

- `dotnet build Piscine.slnx` **vert sous `TreatWarningsAsErrors`** (preuve que l'épingle WebView est
  superflue).
- `dotnet test Piscine.slnx -c Release` → **0 échec**.
- **Répétition locale du packaging** (deploiement.md §5) : publish `Piscine.Desktop` win-x64
  self-contained → libs natives **présentes à la racine** de `desktop/` (noms confirmés en §5).
- **Exécution réelle ici** : `dotnet run --project src/Piscine.Desktop -c Release` → la fenêtre native
  **PhotinoX** s'ouvre et route les pages (preuve sur cette machine Windows ; la fenêtre Linux est
  couverte par les *dry-runs* CI).
- CI verte sur la PR (dry-runs packaging win+linux, AppImage offline lancé hors-ligne, installeur Inno).

## 7. Versionnement & release

- Tag **`v3.1.0`** (SemVer mineur). Pas de fichier de version à bumper (le tag est l'unique source de
  vérité ; cf. deploiement.md §1).
- Flux : PR `(#NN)` → CI verte → merge `main` → commit doc release (CHANGELOG/HANDOFF) → `git tag v3.1.0`
  → `git push origin v3.1.0` → `release.yml` construit et attache **zips win/linux + AppImages
  offline/online + installeurs Windows offline/online** à la **GitHub Release** publique.
- Notes de release dérivées de la section `[v3.1.0]` du CHANGELOG (`gh release edit --notes-file`).

## 8. Plan d'implémentation (esquisse — détaillé dans le plan)

1. **Spike noms natifs** : swap csproj minimal → publish win-x64/linux-x64 → relever les noms de libs.
2. **csproj** : finaliser le swap + retrait de l'épingle WebView ; `build`/`test` verts.
3. **CI/installeurs** : `release.yml` + `ci.yml` (runner/webkit/assertions) + `build-appimage.sh` + `piscine.iss`.
4. **Run réel** local de la fenêtre PhotinoX (Windows).
5. **Docs** : README / deploiement / mise-en-oeuvre + ADR + CHANGELOG `[v3.1.0]`.
6. **PR → CI → merge** ; puis **HANDOFF + retex** ; puis **tag `v3.1.0`** et vérif de la release.
