# Changelog

Toutes les versions notables de la **Piscine .NET**. Format inspiré de
[Keep a Changelog](https://keepachangelog.com/fr/) ; versionnement [SemVer](https://semver.org/lang/fr/).
Le tag git est l'unique source de vérité (cf. [docs/deploiement.md](docs/deploiement.md)).

## [Non publié]

Correctifs post-v4.0.0 (sur `main`, à publier en v4.0.1).

### Corrigé

- **Défilement de la fenêtre Photino** (régression v4.0.0) : le chrome chromeless posait
  `body { overflow: hidden }` (pour arrondir les coins), ce qui empêchait tout défilement → les pages
  longues (`/cours`, etc.) étaient tronquées dans la fenêtre. La fenêtre est désormais un shell fixe
  dont le contenu défile dans `.main` (repli défilement-fenêtre sous 768px). Le DevHost navigateur
  n'était pas concerné, d'où le passage inaperçu.
- **Contrôles de fenêtre (réduire/agrandir/fermer)** : s'appuyaient sur des variables CSS inexistantes
  → quasi invisibles en thème clair et visuellement détachés. Restylés avec les jetons réels du thème,
  pleine hauteur de la barre de titre, collés au coin (intégration façon Discord), cohérents clair/sombre.

### Amélioré

- **Page Vérifier** (`/check`) : le menu d'exercice et le bouton étaient des contrôles HTML bruts →
  menu déroulant et bouton primaire stylés sur les jetons de l'app.
- **Page Terminal** : titre « Terminal embarqué » (retrait du libellé de dev « (spike) »), accents
  corrigés, et panneau de coaching migré sur les jetons du thème (lisible en mode sombre — il était
  codé en couleurs claires).

---

## [v4.0.0] — 2026-06-14

Épic **QoL recrue de bureau** (S0–S8) + **chrome de fenêtre personnalisé** : refonte majeure du
confort d'usage de l'app de bureau Photino, sans toucher au moteur, au CLI `piscine` ni à
`grade-received` (headless `v2.0.0`-compatibles). Le tag git reste l'unique source de vérité de version.

### Ajouté

- **Tableau de bord** (`/`) : carte « Reprendre » (exercice courant / prochain non démarré),
  progression globale (% + compteurs Fait/En cours/À revoir par module) et résultats récents des
  derniers push — dérivés des services existants (`ProgressService`, `PushResultWatcher`).
- **Plan de travail de l'exercice** : barre d'action inline sur chaque page d'exercice — bouton
  **Ouvrir** (éditeur auto-détecté VS Code/Rider/VS, dossier, terminal intégré, terminal système ;
  scaffolding implicite du starter à la première ouverture), bouton **Vérifier** (check in-process),
  pastille de statut.
- **`WorkspaceLauncher` + `EditorResolver`** : auto-détection des éditeurs installés, surcharge via
  `SettingsService`, repli garanti sur l'ouverture du dossier. Lancement via `IProcessLauncher`
  (args en tableau, dossier de workspace résolu — cohérent avec le durcissement sandbox v3.1.1).
- **Palette de commande** (`Ctrl+K` / `⌘K`) : overlay de recherche flou vers tout module, exercice,
  destination et action ; **recherche plein-texte** sur le markdown cours + sujets (index léger en
  mémoire, `SearchService` testable xUnit) ; raccourcis clavier globaux (exo suivant/précédent,
  focus recherche, aller au board).
- **Diff structuré coloré** : `CheckFeedback` rend les cas attendu/obtenu en **diff ligne à ligne
  coloré** (plus de texte verbatim) ; calculé dans `Piscine.App` sans toucher au grader moteur.
- **Toast de push global** : un `ToastHost` dans `MainLayout` s'abonne à `IPushResultWatcher` →
  le verdict s'affiche dans toute l'app dès que le résultat du push arrive (pas seulement sur
  `/résultat`).
- **Page de rapport** (`/rapport`) : recrue + encadrant, lecture seule ; identité git, avancement
  global, tableau par module et historique des push. **Export** : `@media print` → PDF/papier +
  bouton « Copier en Markdown » (rapport git-friendly).
- **Page Réglages** (`/réglages`) : commande éditeur, **thème persistant** (clair/sombre, survit
  au redémarrage de l'app), **échelle de police** — persistés dans `SettingsService` (JSON dans le
  répertoire d'état `PISCINE_HOME`).
- **Onboarding premier lancement** : workspace non initialisé → guide pas-à-pas enrobant
  `InitService` → 1er exercice.
- **Passe lisibilité/accessibilité** sur `piscine.css` : contraste AA, états de focus visibles,
  échelle de police responsive, shell ajusté pour les petits écrans.
- **Navigation enrichie** : pastilles de statut dans l'arbre des modules (sidebar) et dans
  l'onglet *Cours* ; destinations en données (`NavDestinations`) — arbre et onglets sont
  maintenant découplés (migration vers un rail d'icônes type IDE possible sans changer les pages).
- **Chrome de fenêtre personnalisé** (façon Discord) : la barre de titre OS par défaut est retirée
  (fenêtre Photino *chromeless*) et fondue dans la barre de navigation — zone draggable, double-clic
  pour agrandir/restaurer, contrôles **réduire / agrandir / fermer** intégrés à droite. Pilotage natif
  côté hôte `Piscine.Desktop` via messages web ; dans le navigateur (DevHost) les contrôles sont
  masqués. Repli automatique sur le chrome OS sous Linux (WebKitGTK).

### Changé

- **Routage** : `/` → tableau de bord (avant : liste des modules) ; la grille des modules est
  déplacée dans `/cours`.
- **Navigation** : fusion des liens redondants « Curriculum » (barre du haut) et « Accueil »
  (sidebar) dans l'onglet *Tableau de bord*.

---

## [v3.1.1] — 2026-06-09

Version corrective : **durcissement de l'intégrité de la notation** et **isolation de l'exécution du
code recrue**. **Transparent pour la recrue** (même UX, mêmes verdicts) ; CLI headless `piscine` et
`grade-received` **compatibles `v2.0.0`**.

### Sécurité

- **Exécution du code recrue isolée dans un processus enfant jetable** (nouveau projet
  `Piscine.Sandbox`). Une soumission qui dépasse le délai (boucle infinie) est désormais **réellement
  terminée** : le parent **tue l'arbre de processus** au timeout, récupérant thread et assembly.
  Supprime les **fuites de thread/assembly** et la **corruption de sortie inter-exécutions** du modèle
  in-process (`Task.Run` + `task.Wait` jamais annulé). Les fixtures de test `IDisposable`/
  `IAsyncDisposable` sont **disposées** ; **fail-closed** si le bac à sable est indisponible (jamais de
  faux « Réussi », pas de repli in-process).
- **CSP de défense-en-profondeur** sur le shell Desktop (`index.html`) : verrouille les origines de
  chargement (inline du bootstrap/BlazorWebView + cdnjs pour highlight.js autorisés), `object-src
  'none'`, `base-uri 'self'`.
- **Durcissements de notation** : **fail-closed** sur type/cas de notation manquants (plus de faux
  « Réussi »), tolérance d'un `progress.json` corrompu, garde anti-traversal git, hook `post-receive`
  sur ref vide, échappement XSS du markdown de feedback.

### Corrigé

- `GitGrader` honore `HeadRef` (dépôt bare en `grade-received`) ; **validation stricte des clés de
  manifest** : une clé inconnue (typo, ex. `expext_stdout`) est signalée comme problème de contenu au
  lieu d'être ignorée silencieusement.
- Parité `PISCINE_WORKSPACE`, robustesse du watcher de progression et du canal pipe de coaching.
- Test `Grade_BareRepo_WithoutHeadRef` rendu déterministe (indépendant de `init.defaultBranch`).

### Changé

- `RunOutcome.Error` : `Exception?` → `RunError(TypeName, Message)` (l'erreur recrue traverse la
  frontière de processus).
- Maintenance : `.gitattributes` (normalisation LF, source unique de vérité) + bump de l'outillage de
  test.

### Notes

- `Piscine.Sandbox` (exe, dépendances minimales : xunit.core/assert) est publié **self-contained dans
  un sous-dossier `sandbox/`** de chaque bundle — **modèle identique à `Piscine.GitShim`** (indispensable
  pour la distribution **sans SDK**) — et localisé à l'exécution par `SandboxLocator`. Le contrat IPC
  (DTO) est partagé via `Piscine.Sandbox.Contracts` : `Piscine.Grading` s'y lie **sans référencer l'exe**
  (un `ProjectReference` vers un exe casse le publish self-contained, NETSDK1152). Les dépendances
  managées/natives du code recrue (Microsoft.Extensions.\*, Microsoft.Data.Sqlite + `e_sqlite3`) sont
  résolues via les chemins runtime du processus de correction.
  Spec/plan/retex : `superpowers/{specs,plans,retex}/2026-06-09-grading-sandbox-isolation*`.
- ⚠️ Si `Piscine.Sandbox` était un jour publié **trimmed/AOT**, le code recrue utilisant la
  sérialisation JSON **par réflexion** casserait — à documenter au packaging.

## [v3.1.0] — 2026-06-08

Migration d'infrastructure de l'app de bureau : passage du paquet **`Photino.Blazor 3.2.0`** au fork
maintenu **`PhotinoX.Blazor 4.2.0`** (net10-natif). **Transparent pour la recrue** (même UX, mêmes
pages) ; le moteur de notation, le **CLI headless `piscine`** et `grade-received` restent **compatibles
`v2.0.0`**.

### Changé

- **`Piscine.Desktop`** : `Photino.Blazor 3.2.0` → **`PhotinoX.Blazor 4.2.0`**. Le fork aligne
  `Microsoft.AspNetCore.Components.WebView` sur **10.0.x** → l'**épingle manuelle `10.0.8`** et le
  contournement **NU1605** sont supprimés. API (`PhotinoBlazorAppBuilder`, namespace `Photino.Blazor`)
  inchangée → **aucun changement de code applicatif** (hormis un fallback non-null sur `ShowMessage`).
- **Libs natives** renommées dans le paquet : `PhotinoX.Native.dll` (Windows) / `PhotinoX.Native.so`
  (Linux), `WebView2Loader.dll` conservé. Toujours à la **racine** de `desktop/`.
- **Linux — webkit2gtk-4.0 → 4.1** : le job `package-linux` passe à **`ubuntu-24.04`**. **Prérequis Linux
  (zip + AppImage)** : `libwebkit2gtk-4.1` (`apt install libwebkit2gtk-4.1-0`), au lieu de `4.0`.

### Retiré

- **AppImage Linux *offline* abandonnée.** WebKitGTK en build release (distribution) **ignore**
  `WEBKIT_EXEC_PATH` (honoré uniquement en `DEVELOPER_MODE`) et localise ses process auxiliaires
  (`WebKitNetworkProcess`/`WebKitWebProcess`) via un chemin **absolu compilé** → un AppImage ne peut pas
  embarquer un webkit fonctionnel sans bind-mount privilégié. Seule l'**AppImage online** (webkit
  **système**) est désormais publiée. La release v3.1.0 attache donc **5 artefacts** (zips win/linux,
  installeurs Windows offline/online, AppImage Linux online). *(Constat valable aussi pour webkit 4.0 ;
  l'ancienne AppImage offline ne rendait en réalité que sur un poste disposant déjà du webkit système.)*

### Notes

- ADR : [docs/superpowers/adr/2026-06-08-photinox-fork.md](docs/superpowers/adr/2026-06-08-photinox-fork.md).
- Windows : runtime **WebView2** inchangé (Evergreen, géré par les installeurs).

## [v3.0.0] — 2026-06-07

Release majeure : **nouvelle UX recrue de bureau** (app Photino.Blazor) en plus du CLI, **installeurs**
Windows + Linux (offline & online), et **notation live des exercices git** au push. Le moteur de notation
console reste compatible `v2.0.0`. **macOS n'est plus distribué.**

### Ajouté — Application de bureau (Photino.Blazor)

Nouvelle **UX recrue de bureau** qui complète (et peut remplacer) le CLI console, **sans changer la
logique de notation** : graders, **CLI headless `piscine`** et le verdict de `grade-received`
(hook `post-receive`) restent **compatibles** `v2.0.0`.

- **App `Piscine.Desktop`** (Photino.Blazor, fenêtre native) : lecteur de **cours/sujets** (sommaire,
  coloration syntaxique, mode sombre), **vérification** instantanée d'un exercice (page *Vérifier*, ne
  compte pas comme rendu, avec diff/indice/lien cours), **progression** par exercice (*Progression*),
  **initialisation** du workspace (*Initialiser*), **terminal embarqué + coaching git** (page *Terminal*),
  **résultat** de push **riche** auto-rafraîchi (*Résultat* : verdict + diff + indice + lien cours).
  Composants/services partagés dans la bibliothèque `Piscine.Components` (consommée aussi par le site de
  dev `Piscine.DevHost`, hors release).
- **Terminal embarqué + coaching git** : la page *Terminal* lance un vrai shell (PTY) et un **coaching
  éducatif** qui réagit aux commandes git (sans parser le stdout : un **shim `git`** émet `{argv,exitCode,cwd}`
  sur un canal IPC). Le shim est livré dans `desktop/gitshim/`. *(Prouvé sur Windows et sur Linux via Docker.)*
- **Résultat de push riche** : `grade-received` persiste, en plus de `progress.json`, un
  `last-push-result.json` (par exercice : statut + diff verbatim + indice + lien cours) — **sans changer
  la logique de notation ni le stdout** ; la page *Résultat* le rend inline (rétro-compat statut-only).
- **Packaging** — Windows + Linux (**macOS abandonné**) :
  - **Installeurs** (recommandés) : Windows `.exe` **per-utilisateur** (offline = runtime WebView2
    embarqué / online = bootstrapper) ; Linux **AppImage** (offline = webkit2gtk-4.0 bundlé, **hors-ligne** /
    online = webkit système).
  - **Zips** self-contained conservés (`win-x64`, `linux-x64`) : CLI `piscine` + app de bureau `desktop/`
    (+ `gitshim/`) + `content/` + MinGit (Windows). Libs natives Photino à la **racine** de `desktop/`.
  - **Dry-runs CI** à chaque PR : publish desktop + libs natives, AppImage offline **lancé hors-ligne**,
    installeur Windows compilé (Inno).
- **Prérequis webview** par OS — gérés par les installeurs ; en mode zip : Windows **WebView2** /
  Linux **`libwebkit2gtk-4.0`** (Photino 3.2.0 — **pas** 4.1). Voir
  [docs/mise-en-oeuvre.md](docs/mise-en-oeuvre.md) et [docs/deploiement.md](docs/deploiement.md).

### Moteur — notation live des exercices `git`

- **`grade-received` note désormais les exercices `git` au push**, contre le **dépôt bare** (l'historique
  reçu), via un « HEAD effectif » sur la branche de rendu (`main`). Un **signal « tenté »** (`attempt` :
  présence d'une branche/d'un fichier) évite tout « à revoir » parasite sur les exos non commencés ;
  l'exo M05 (`ex00-branche-merge`) est noté en live (penser à `git push origin --all`). Aucun changement
  aux autres graders ni au CLI.

### Limites connues

- **macOS** n'est plus distribué (pas de runner pour prouver la fenêtre native / webview WKWebView non
  automatisable en CI).
- **AppImage Linux** : pour le `git push` du rendu, préférer le **terminal système** (le hook a besoin
  d'un git stable, le montage AppImage est éphémère). Le terminal embarqué + coaching restent disponibles.

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

[v3.0.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v3.0.0
[v2.0.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v2.0.0
[v1.0.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v1.0.0
[v0.1.0]: https://github.com/Benjamin-Curlier/piscine-dotnet/releases/tag/v0.1.0
