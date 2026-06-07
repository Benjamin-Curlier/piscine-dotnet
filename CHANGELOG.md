# Changelog

Toutes les versions notables de la **Piscine .NET**. Format inspiré de
[Keep a Changelog](https://keepachangelog.com/fr/) ; versionnement [SemVer](https://semver.org/lang/fr/).
Le tag git est l'unique source de vérité (cf. [docs/deploiement.md](docs/deploiement.md)).

## [Non publié]

### Ajouté — Application de bureau (Photino.Blazor)

Nouvelle **UX recrue de bureau** qui complète (et peut remplacer) le CLI console, **sans changer le
moteur** : graders, **CLI headless `piscine`** et `grade-received` (hook `post-receive`) restent
**identiques** à `v2.0.0`.

- **App `Piscine.Desktop`** (Photino.Blazor, fenêtre native) : lecteur de **cours/sujets** (sommaire,
  coloration syntaxique, mode sombre), **vérification** instantanée d'un exercice (page *Vérifier*, ne
  compte pas comme rendu), **progression** par exercice (*Progression*), **initialisation** du workspace
  (*Initialiser*), **résultat** de push auto-rafraîchi (*Résultat*). Composants/services partagés dans la
  bibliothèque `Piscine.Components` (consommée aussi par le site de dev `Piscine.DevHost`, hors release).
- **Packaging** : `Piscine.Desktop` est livré **self-contained par OS** dans le zip (dossier `desktop/`
  + lanceur `start-piscine-desktop`), **à côté** du CLI `piscine` inchangé, du `content/` et de MinGit
  (Windows). Un **dry-run CI** publie les 3 RID et vérifie la présence des libs natives à chaque PR.
- **Prérequis webview** par OS documentés (Windows **WebView2** / Linux **`libwebkit2gtk-4.1`** /
  macOS **WKWebView**) — voir [docs/mise-en-oeuvre.md](docs/mise-en-oeuvre.md) et
  [docs/deploiement.md](docs/deploiement.md).

### Limites connues

- **Terminal embarqué** et **coaching git** dans l'app **différés** : le rendu (`git add/commit/push`)
  se fait au **terminal système** (sous Windows, `start-piscine.cmd` met `git` + `piscine` sur le PATH).

## [v2.0.0] — 2026-06-06

Release cumulative depuis `v1.0.0` : nouveaux graders, deux paliers de contenu (approfondissement
C#/.NET et plateformes & architecture), et déblocage de modules restés en attente.

### Graders (moteur)
- **`mutation`** — l'élève écrit ses propres tests xUnit, confrontés à une impl. de référence cachée
  et à des mutants nommés dans le manifest ; verdict binaire (tous les mutants tués). Pilote **M13**
  (`ex03-mutation`).
- **`git`** — verdict sur l'**état attendu du dépôt rendu** (branches, `min_commits`, ancêtre fusionné,
  contenu de fichiers, absence de marqueurs de conflit), inspecté via LibGit2Sharp. Corrigé décrit par
  une **fixture** déclarative validée par `validate-content`. **M05** devient auto-noté.
- **`projet`** — compilation **multi-fichiers** + cas `io` optionnels + **assertions d'architecture
  Roslyn** (`requires_types`, `forbidden_dependencies` namespace→namespace). Pilote **M36**.
- **`reseau`** — fondation moteur : harnais d'écho TCP loopback, injection host/port en arguments,
  comparaison `io`.

### Contenu
- **Palier v2 — approfondissement C#/.NET** : **M24–M35** (switch & pattern matching, enums,
  static/const/immutabilité, opérations binaires, complexité & tris, recherche de chemin BFS/Dijkstra/A*,
  design patterns suite, refactoring, GC & ressources, discriminated unions, interop *(lecture)*,
  EF Core SQLite in-memory). Introduit le champ `difficulty` et des **exercices bonus non bloquants**.
- **Palier v3 — plateformes & architecture** : **M36** Clean Architecture *(auto-noté `projet`)*,
  **M37** Docker, **M38** Silk.NET, **M39** Blazor *(lecture guidée — sortie non déterministe)*.
- **M19 (Logging)** et **M20 (Generic Host & Worker)** débloqués en **`io`** (provider de log synchrone
  fourni → sortie déterministe captée par le grader, sans changement moteur).
- **Rush 3** (`r3-traitement`) — Worker Service déterministe auto-noté (`Channel<T>` + DI + LogCapture,
  arrêt via `StopApplication()`).

### Outillage & infra
- **`piscine try <exo>`** — outil auteur qui exécute le corrigé et imprime le `stdout` réel au format
  YAML collable (fini de deviner les `expect_stdout`) ; étendu aux cas `projet`.
- **Site du cours** `src/Piscine.Web` (Blazor SSR, mode sombre) : cours + sujets dans le navigateur,
  réutilise `Piscine.Core`. Buildé par la CI, **hors release**.
- **164 tests** verts (Core 46 + Git 7 + Grading 111) ; CI verte sur `main`.

## [v1.0.0] — Curriculum complet

- Tronc commun + palier avancé : **M00–M23** + **Rushes 0/1/2**.
- Moulinette auto-correctrice locale (graders `io` / `unit` / `norme`, Roslyn embarqué), rendu par
  **vrai git** (dépôt bare local + hook `post-receive`), correction par groupe séquentielle.
- Distribution **self-contained** par OS (win-x64, linux-x64, osx-arm64) ; MinGit portable bundlé
  sous Windows. Packaging `Microsoft.Extensions.*` + EF Core SQLite.
- M14 (git avancé) et M22 (réseau) en lecture guidée ; blocages moteur identifiés et branchés ensuite.

## [v0.1.0] — Bootstrap

- Amorçage du moteur et de la structure de contenu.

[v2.0.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v2.0.0
[v1.0.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v1.0.0
[v0.1.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v0.1.0
