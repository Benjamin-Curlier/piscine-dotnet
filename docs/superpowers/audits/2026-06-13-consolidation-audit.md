# Audit de consolidation — piscine-dotnet

> **Date** : 2026-06-13 · **Branche** : `consolidation/audit` · **Baseline** : `main` @ `c8171e9`
> (**v3.1.1**). 11 projets `src/` + 6 projets de test, **305 tests**, arbre propre.
> **Périmètre** : lecture seule + mesures locales transitoires. **Aucun** changement de code/CI/contenu,
> **aucune** issue créée, **aucun** tag. Méthode et décisions : `specs/2026-06-13-consolidation-audit-design.md`.
> Sévérité : **P0** correction/sécurité · **P1** forte valeur · **P2** utile · **P3** cosmétique.
> Effort : **S** ≤ ½ j · **M** ~1-2 j · **L** > 2 j.

> ⚠️ **Note de re-baseline** : cet audit a d'abord été cadré sur un checkout local **périmé de 23 commits**
> (HANDOFF/mémoire décrivant v3.0.0 / 267 tests / 9 projets). Après `git fetch`, la vérité = **v3.1.1** :
> migration **PhotinoX.Blazor 4.2.0** (webkit2gtk-4.1), **isolation processus enfant** (`Piscine.Sandbox` +
> `Piscine.Sandbox.Contracts`), durcissements sécurité (#58), **305 tests**, **5 artefacts** de release
> (AppImage *offline* abandonnée). Les chiffres ci-dessous portent sur ce vrai arbre.

---

## Résumé exécutif

_(complété en synthèse — Tâche 6)_

## Table d'actions priorisée

_(complété en synthèse — Tâche 6)_

---

## 1. CI

**Workflows** : `ci.yml` (4 « jobs » : `build-test`, `appimage-online-dryrun`, `windows-installer-dryrun`)
sur `push`/`pull_request` vers `main` ; `release.yml` (`package-linux`, `package-windows`, `release`) au tag `v*`.

**Durées réelles** (12 derniers runs `ci.yml` sur `main`, wall-time) : **1 à 4 min**. Les jobs tournent en
parallèle ; `build-test` (~3 min) domine, `appimage-online-dryrun` ~1 min, `windows-installer-dryrun` ~0-1 min.
Le CI est donc **déjà rapide** — l'optimisation porte surtout sur le **gaspillage** (runs inutiles, doublons),
pas sur la latence d'un run vert.

### Constats

| ID | Constat | Sév. | Effort |
|----|---------|------|--------|
| **CI-1** | **Dry-runs lourds sur les commits docs-only.** Les 3 gardes de packaging (publish cross-RID Desktop+Sandbox, AppImage online, installeurs Windows) s'exécutent sur **chaque** push/PR, y compris les nombreux commits purement documentaires de la boucle SCRUM (`docs: consigner …`). **Preuve** : le run `27210660012` sur le commit **`docs(release): consigner v3.1.1`** (diff = docs only) a lancé toute la matrice et a **échoué** aux étapes *« Dry-run packaging desktop »* et *« Construire l'AppImage online »* (régression de packaging entrée plus tôt, réparée au commit suivant `c8171e9`). Un commit de doc n'aurait pas dû exécuter — ni rougir sur — le packaging. | **P1** | **S** |
| **CI-2** | **Pas de `concurrency:`.** Des pushes successifs rapides (fréquents : impl + « consigner » qui suit) laissent des runs redondants finir au lieu d'être annulés. Ajouter `concurrency: { group: ci-${{ github.ref }}, cancel-in-progress: true }`. | **P2** | **S** |
| **CI-3** | **Pas de cache NuGet.** `actions/setup-dotnet@v5` est utilisé sans `cache: true` ; chaque job re-`restore` à froid (4 jobs × chaque run). Gain modeste (~20-40 s/job vu les durées actuelles) mais quasi gratuit à activer (`cache: true` + `cache-dependency-path`). NB : sans `packages.lock.json`, la clé de cache est moins fiable — envisager `RestorePackagesWithLockFile`. | **P2** | **S** |
| **CI-4** | **Publish linux-x64 dupliqué entre jobs.** `Piscine.Desktop` + `Piscine.Sandbox` (linux-x64, self-contained) sont publiés **à la fois** dans `build-test` (dry-run packaging) **et** dans `appimage-online-dryrun`. Isolation des jobs = choix défendable, mais c'est du calcul redondant à chaque run. Faible priorité (les deux jobs sont courts). | **P3** | **M** |
| **CI-5** | **`release.yml` n'a pas de `concurrency` ni de garde de pré-vol.** Un tag déclenche directement 3 jobs de packaging réels (téléchargements MinGit/WebView2, Inno, AppImage) sans étape « les tests passent ? » en amont (les tests ne tournent qu'au `push` sur `main`, pas au tag). Un tag posé sur un commit non testé publierait quand même. Envisager un job `needs:`-gate qui rejoue `build-test`. | **P2** | **S** |

### Recommandation prioritaire (CI)

**CI-1 d'abord (P1/S, plus gros gain)** : filtrer les 3 gardes de packaging par chemins — ne les déclencher
que si `src/**`, `build/installer/**`, `**/*.csproj`, `.github/workflows/**`, `Directory.Build.props` changent ;
laisser `build-test` + `validate-content` sur tous les commits. Sur ce repo (≈ 1 commit « consigner »
documentaire pour chaque commit d'impl), cela supprime ~la moitié des exécutions de la matrice lourde.
Puis CI-2 et CI-3 (quick wins). CI-5 si on veut blinder la release. CI-4 optionnel.
