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

---

## 2. Couverture de tests

**Mesure réelle** : `dotnet test Piscine.slnx -c Release --collect:"XPlat Code Coverage"` (305 tests verts,
E2E inclus) + ReportGenerator 5.5.10. **Global : 78,0 % lignes · 62,8 % branches · 82,3 % méthodes**
(2607/3341 lignes, 988/1571 branches). 7 assemblies instrumentées.

**Couverture par assembly** :

| Assembly | Lignes | Lecture |
|----------|:------:|---------|
| `Piscine.Core` | **98,8 %** | Logique pure (modèles, découverte de contenu, YAML) — excellemment testée. |
| `Piscine.Git` | **94,6 %** | `GradeReceivedCommand` 98 %, `GitWorkspace` 78 %. Solide. |
| `Piscine.App` | **86,0 %** | Services UI. Bas : `ShimLocator` 60 %, `PtyService` 73 % (terminal, dur à tester sans tête). |
| `Piscine.Sandbox.Contracts` | **86,1 %** | Contrat IPC (DTO + source-gen JSON). OK. |
| `Piscine.Grading` | **85,9 %** | Cœur moteur. **Bas et critique** : `SandboxLauncher` **53,8 %**, `SandboxProcess` 76 %, `ContentValidator` **69,1 %**. |
| `Piscine.Sandbox` | **63,9 %** | **Le plus bas du moteur.** `SandboxExecutor` 62 %, `Program` 50 % (entrée du processus enfant). |
| `Piscine.Components` | **29,5 %** | **Trompeur** : pages/layout (`Home`/`Module`/`NavMenu`/`Terminal`/`CourseCatalog`…) à 0 % car couvertes par **DevHost.E2E hors-process** (non instrumenté). Les composants testés en **bUnit** sont hauts (`MarkdownView` 100 %, `CourseToc` 96 %, `PushResultPanel` 94 %, `StatusBadge` 92 %, `InitPanel` 88 %, `CheckFeedback` 86 %). |

**Non instrumentés du tout** : `Piscine.Cli`, `Piscine.Desktop`, `Piscine.DevHost`, `Piscine.GitShim`
(projets d'entrée/hôte, exécutés hors-process → couverts par E2E/smoke, pas par la couverture unitaire).
Le « 78 % » porte donc sur le **moteur + services**, où il est réellement fort.

### Constats

| ID | Constat | Sév. | Effort |
|----|---------|------|--------|
| **COV-1** | **Le code de sécurité le moins couvert.** `Piscine.Sandbox` (63,9 %) + `Grading.SandboxLauncher` (53,8 %) / `SandboxProcess` (76 %) forment la frontière d'isolation **fail-closed** du code recrue non fiable (v3.1.1) — précisément le code dont une régression silencieuse serait grave. Cibler les branches *timeout / kill-tree / fail-closed / bac-à-sable-absent* de `SandboxExecutor` et `SandboxLauncher`. (Caveat : `Sandbox/Program` 50 % tourne hors-process → couverture d'intégration ou acceptée via E2E.) | **P1** | **M** |
| **COV-2** | **`ContentValidator` 69,1 %** — la **gate de contenu** (plus gros fichier, 367 l.) qui empêche un corrigé cassé de passer. Couverture basse pour un fort rayon d'impact (chaque exo dépend d'elle). Ajouter des cas (manifestes invalides, fixtures git, chemins d'erreur). | **P2** | **M** |
| **COV-3** | **Aucune mesure de couverture en CI.** `coverlet.collector` est présent (défaut du gabarit xUnit) mais jamais invoqué : rien ne suit le 78 % dans le temps, aucun garde-fou de régression, aucune visibilité PR. Ajouter `--collect:"XPlat Code Coverage"` + résumé ReportGenerator en artefact (ou job summary). **Pas de seuil bloquant** (cohérent avec l'éthos « jamais de note » + projets hôtes hors couverture) — viser la **visibilité**, pas une barrière. | **P3** | **S** |

### Lecture

La couverture est **saine** là où ça compte le plus (Core 98,8 %, Git 94,6 %, moteur de notation 86 %).
Le point d'attention réel = le **nouveau code sécurité (Sandbox)**, le moins couvert alors qu'il est le plus
sensible. Le 62,8 % de branches (vs 78 % de lignes) confirme que ce sont les **chemins d'erreur/bord** qui
manquent, pas le chemin nominal. Priorité : **COV-1** (sécurité), puis **COV-3** (rendre le chiffre visible
en CI, quasi gratuit), puis **COV-2**.
