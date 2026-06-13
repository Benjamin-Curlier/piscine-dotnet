# Spec — Épic « QoL recrue » de l'app de bureau

> Design validé en brainstorming le 2026-06-13. Cible : améliorer le **confort d'usage (QoL)** de
> l'app de bureau Photino pour la recrue — tableau de bord, navigation fluide cours ↔ board ↔ exo,
> ouverture d'un exercice dans l'IDE / le terminal / le dossier, page de rapport dédiée, et une passe
> de lisibilité/ergonomie — **sans toucher au moteur, au CLI headless `piscine`, ni à `grade-received`**.
> À lire avec [HANDOFF](../HANDOFF.md), la spec [v4 Photino Desktop](2026-06-06-v4-photino-desktop-design.md)
> et la spec [PhotinoX](2026-06-08-photinox-migration-design.md).

## 1. Contexte & motivation

Depuis v4/v5, l'**app de bureau** (`Piscine.Desktop`, hôte Photino qui rend la RCL
`Piscine.Components`, adossé aux services de `Piscine.App`) est l'UX recrue principale : elle présente
le cours, vérifie en in-process (`/check`), suit la progression (`/progress`), initialise le workspace
(`/init`), surveille le résultat de push (`/resultat`) et embarque un terminal + coaching git
(`/terminal`). Le **moteur** (`Core`/`Grading`/`Git`), le **CLI `piscine`** et le hook `grade-received`
restent la source de vérité de la correction et **ne bougent pas**.

Mais la surface recrue accuse plusieurs faiblesses d'IHM, constatées en lisant les composants :

- **Le langage « console » fuit dans une app de bureau.** Le hero de l'accueil dit *« chaque sujet se
  fait en ligne de commande avec `piscine start` »* et la page d'exercice prescrit *« Pour démarrer :
  `piscine start <exo>` … `piscine check` »* — alors que l'app sait déjà démarrer/vérifier/ouvrir un
  terminal. L'UI invite la recrue à **quitter l'app**.
- **La navigation ne porte aucun statut.** L'arbre des modules (NavMenu) et la grille de modules
  (Home) n'affichent aucun signal de progression ; le statut n'existe que sur `/progress`.
- **Navigation redondante et plate.** Le lien « Curriculum » (barre du haut) et « Accueil » (sidebar)
  pointent tous deux vers `/` ; la sidebar empile 6 liens d'action et l'arbre des modules sans
  hiérarchie, et il n'y a **pas de tableau de bord**.
- **La page d'exercice est en lecture seule.** C'est un lecteur de sujet avec préc./suiv. — aucune
  action (vérifier, ouvrir, statut) là où la recrue lit réellement son travail.
- **Le diff n'est pas un diff.** `CheckFeedback` rend les messages du grader **verbatim** comme texte ;
  attendu/obtenu n'est pas un vrai diff visuel.

L'épic « QoL » corrige ces points et ajoute le confort attendu d'une vraie app de bureau.

## 2. Objectifs / Non-objectifs

**Objectifs**
- Un **tableau de bord** (`/`) qui oriente la recrue d'un coup d'œil : prochaine action → progression
  globale → résultats récents.
- Une **navigation fluide** cours ↔ board ↔ exercice, avec le **statut tissé dans la navigation**.
- Un **bouton « Ouvrir »** par exercice : **IDE** (auto-détecté, surchargeable), **dossier**,
  **terminal intégré** et **terminal système** — avec scaffolding implicite du starter à la 1ʳᵉ ouverture.
- Une **page de rapport dédiée** (`/rapport`), pour la **recrue ET l'encadrant**, **exportable**
  (impression/PDF + Markdown).
- Une **palette de commande** (`⌘K`/`Ctrl+K`) + recherche plein-texte + raccourcis clavier.
- Une **boucle de retour enrichie** : vrai diff coloré, toast de résultat de push global, activité récente.
- Une **page Réglages** + persistance du thème + mise à l'échelle de la police, et une **passe
  lisibilité/accessibilité** + un **onboarding au 1ᵉʳ lancement**.
- **0 avertissement de build** (`TreatWarningsAsErrors`), même pyramide de tests qu'en v4/v5.

**Non-objectifs**
- Aucun changement du **moteur**, des graders, du format de contenu, du **CLI `piscine`**, du rendu git
  ni de `grade-received`. Aucun changement de `release.yml` (l'app est déjà packagée).
- Pas d'éditeur de code embarqué (l'IDE reste externe — décision v4).
- Pas de retour de macOS (abandonné en v5 ; cibles = Windows + Linux).
- Pas de nouvelle persistance pour les données dérivées (board, dots, rapport = lecture seule sur des
  services existants). Seul ajout : un petit `SettingsService`.
- Pas de bascule immédiate vers le rail d'icônes type IDE (cf. §6 « A maintenant, B plus tard »).

## 3. Décisions actées (brainstorming 2026-06-13)

1. **Page de rapport** : deux audiences (recrue + encadrant), **une seule page**, conçue pour
   l'**export** — feuille de style d'impression (PDF/papier) **et** export **Markdown**.
2. **Tableau de bord** : composition **équilibrée**, dans l'ordre **prochaine action → progression
   globale → résultats récents**.
3. **Ouvrir dans l'IDE** : **auto-détection** des éditeurs installés (VS Code, Rider, Visual Studio)
   **+ surcharge** d'une commande dans les Réglages ; **repli sur « ouvrir le dossier »** si rien n'est
   trouvé.
4. **Ouvrir en CLI** : proposer **les deux** — **terminal intégré** (action principale, `cwd` = dossier
   de l'exo, garde le coaching git) **et** **terminal système** (option secondaire).
5. **Modèle de navigation** : **Approche A** (onglets en haut + arbre des cours dans la sidebar, board
   par défaut, palette `⌘K`), **construite pour migrer plus tard vers B** (rail d'icônes) à moindre coût.
6. **Bundles QoL retenus (tous)** : (1) progression tissée dans la nav, (2) palette + recherche +
   raccourcis, (3) boucle de retour enrichie, (4) polish/réglages/lisibilité.
7. **Première étape de l'épic = audit UX en conditions réelles** (lancer l'app, capturer chaque page,
   cataloguer les vrais problèmes de lisibilité/ergonomie) pour concevoir contre la réalité.

## 4. Architecture & garde-fous

- **Où vit le code** : toute l'UI nouvelle va dans la RCL **`Piscine.Components`** (testable via
  `Piscine.DevHost`) ; toute la logique va dans des services de **`Piscine.App`**. **Moteur, `Piscine.Cli`,
  `grade-received` et `release.yml` ne sont pas touchés** (même discipline qu'en v4/v5).
- **Pas de nouvelle persistance dérivée** : le board, les pastilles de statut et le rapport lisent des
  services **déjà existants** — `ProgressService`, `PushResultWatcher`/`last-push-result.json`,
  `GitStatusService`/`RepoState`, `CourseCatalog`. Seul nouvel état persistant : `SettingsService`
  (JSON dans le répertoire d'état) pour la commande éditeur, le thème, l'échelle de police, la cible
  terminal par défaut.
- **Sécurité** : tout lancement de processus (éditeur, terminal système, gestionnaire de fichiers)
  passe les arguments **en tableau** (jamais de concaténation de chaîne), et ne cible **que** le dossier
  de workspace résolu — cohérent avec l'historique de durcissement bac à sable (#58, `Piscine.Sandbox`).
- **Build** : `TreatWarningsAsErrors` conservé, 0 warning ; CRLF Windows bénins (`.editorconfig` = LF).

## 5. Surfaces

### 5.1 Coquille de navigation (Approche A)
- **Destinations primaires en données** : une liste ordonnée `{ icône, libellé, route, testid }` —
  *Tableau de bord, Cours, Progression, Rapport, Terminal, Réglages*. `MainLayout` la rend en **barre
  d'onglets en haut**. (Cf. §6 pour la migration B.)
- **Routage** : board sur `/` (atterrissage par défaut) ; la grille de modules actuelle devient le
  catalogue **« Cours »** sur `/cours`. Les redondances « Curriculum » (barre) + « Accueil » (sidebar)
  fusionnent dans l'onglet *Tableau de bord* (+ marque → board).
- **Sidebar** = **arbre des cours** (modules → exercices) avec **pastilles de statut** (via
  `ProgressService`), affichée en contexte *Cours*/*Exercice*.

### 5.2 Tableau de bord (`/`)
Équilibré, dans l'ordre **prochaine action → progression → résultats récents** :
- **Carte « Reprendre »** : CTA principal vers l'exercice courant (dernier en cours / prochain non
  démarré) ouvrant le **plan de travail** de l'exercice ; dérivé de `ProgressService` + `RepoState`.
- **Progression globale** : avancement % + compteurs (Fait / En cours / À revoir / Restant) + mini-barres
  par module ; via `ProgressService.SnapshotFor(tous)`.
- **Résultats récents** : derniers verdicts de push (via `PushResultWatcher`/`last-push-result.json`),
  avec lien vers le diff (`/check`) ou l'indice.

### 5.3 Plan de travail de l'exercice (upgrade de `Exercise.razor`)
- Une **barre d'action** remplace la consigne CLI : **Ouvrir** (menu : éditeur détecté / Ouvrir le
  dossier / Terminal intégré / Terminal système), **Vérifier** (check in-process inline), **pastille de
  statut**. Sujet + sommaire (`CourseToc`) + pager préc./suiv. conservés.
- Le **starter est copié au workspace à la 1ʳᵉ ouverture** (équivalent in-app de `piscine start`).

### 5.4 Service « Ouvrir » — `WorkspaceLauncher` (`Piscine.App`)
- Résout `PiscineLayout.WorkspaceExerciseDir(module, exo)` ; **scaffolde via `StarterInstaller.Install`
  si absent**.
- **Dossier** : gestionnaire de fichiers de l'OS (`explorer` / `xdg-open`).
- **Éditeur** : `EditorResolver` sonde le PATH + emplacements connus (`code`, `rider`, VS) ; **surcharge
  Réglages** ; **repli → dossier** si rien trouvé.
- **Terminal intégré** : navigue vers `/terminal?cwd=<dir>` (xterm + coaching existants).
- **Terminal système** : lance le shell de l'OS dans le dossier.
- Lancement abstrait derrière **`IProcessLauncher`** (testable : on asserte commande+args résolus sans
  réellement spawn).

### 5.5 Page de rapport (`/rapport`)
- **Recrue + encadrant**, une page, en lecture seule sur les services existants.
- En-tête : identité git (`RepoState` → `user.name`/`email`), date de génération, avancement global.
- Tableau **par module** : exercices faits / en cours / à revoir, mix difficulté, complétion des bonus.
- Historique des push récents / verdicts.
- **Export** : feuille de style `@media print` → `window.print()` (PDF/papier) **+** bouton
  « Copier / Enregistrer en Markdown » générant un rapport git-friendly.

### 5.6 Palette de commande + recherche (bundle 2)
- Overlay **`⌘K`/`Ctrl+K`** : saut flou vers tout module/exercice/destination/action (Vérifier, Ouvrir,
  Initialiser…).
- **Recherche plein-texte** sur le markdown cours + sujets (index léger en mémoire bâti depuis
  `CourseCatalog`).
- **Raccourcis clavier** globaux (exo suivant/précédent, focus recherche, aller au board).
- Composant `CommandPalette` (RCL) monté dans `MainLayout` ; interop JS pour le hotkey global + piège
  de focus.

### 5.7 Boucle de retour enrichie (bundle 3)
- **Diff structuré** : `CheckService` recompose déjà le résultat in-process → exposer un **attendu/obtenu
  structuré** rendu en **diff coloré ligne à ligne** dans `CheckFeedback` (remplace le texte verbatim).
  C'est le seul endroit nécessitant une petite évolution **côté `Piscine.App`** (pas le grader du moteur).
- **Toast de push global** : un `ToastHost` dans `MainLayout` s'abonne au `PushResultWatcher` existant →
  un verdict s'affiche **partout** dans l'app (pas seulement sur `/resultat`).
- **Activité récente** : petite liste d'historique alimentant board + rapport.

### 5.8 Réglages, onboarding & lisibilité (bundle 4)
- **`SettingsService`** (JSON dans le répertoire d'état) : commande éditeur, thème, échelle de police,
  cible terminal par défaut → page **`/reglages`**.
- **Onboarding 1ᵉʳ lancement** (enrobe `InitService`) : workspace non initialisé → init guidé → 1ᵉʳ exo.
- **Passe lisibilité/accessibilité** sur `piscine.css` : contraste, états de focus, échelle de police,
  responsive — la partie qui répond directement à « l'UI est lisible et utilisable ».

## 6. Modèle de navigation — « A maintenant, B plus tard »

Le découplage **destinations-en-données** (§5.1) sépare les *destinations* de leur *présentation* :
- **Aujourd'hui (A)** : `MainLayout` rend la liste en **barre d'onglets** + sidebar = arbre des cours.
- **Plus tard (B)** : un `RailLayout` rend la **même liste** en **rail d'icônes type IDE** (activity
  bar) basculant un panneau contextuel — **sans changement de page**. La bascule reste une décision
  ultérieure, hors périmètre de cet épic.

## 7. Découpage en sprints (1 issue = 1 sprint, rythme V3/v4/v5)

| Sprint | Périmètre |
|--------|-----------|
| **S0** | Audit UX réel (capture de chaque page, catalogue des vrais problèmes) + fondation nav : destinations-en-données, fusion de la nav redondante, routage (`/`→board, `/cours`→catalogue), plomberie des pastilles de statut |
| **S1** | Tableau de bord (`/`) |
| **S2** | Plan de travail de l'exercice + `WorkspaceLauncher` + `EditorResolver` (+ store Réglages minimal pour la surcharge éditeur) — **fonction phare** |
| **S3** | Palette de commande + recherche + raccourcis clavier |
| **S4** | Boucle de retour enrichie (diff structuré, toast de push, activité récente) |
| **S5** | Page de rapport + export (print CSS + Markdown) |
| **S6** | Page Réglages + persistance du thème + échelle de police |
| **S7** | Passe lisibilité/a11y + onboarding 1ᵉʳ lancement (capstone, informé par l'audit S0) |
| **S8** | Docs + CHANGELOG + guides recrue/encadrant |

**Dépendances notables** : la surcharge éditeur (Réglages) est requise par S2 → un **store Réglages
minimal** atterrit en S2 ; la **page** Réglages complète en S6. S7 s'appuie sur l'audit S0.

## 8. Tests & non-fonctionnel

- **Pyramide identique** à v4/v5 :
  - **xUnit** (`Piscine.App`) : `WorkspaceLauncher`, `EditorResolver`, `SettingsService`, index de
    recherche, diff structuré.
  - **bUnit** (`Piscine.Components`) : board, barre d'action, palette, rapport, toast, diff.
  - **Playwright E2E** (`Piscine.DevHost.E2E`) : palette → saut, « ouvrir le dossier » mocké
    (`IProcessLauncher`), export du rapport, « Reprendre » du board. Run solution verte **sans**
    navigateur (skip CI ubuntu).
- **Ouverture OS side-effecting** → derrière `IProcessLauncher` ; fenêtre native = **smoke manuel par
  OS** (Windows + Linux), cohérent avec la discipline Photino.
- **Invariants** : 0 warning ; moteur/CLI/`grade-received`/`release.yml` intacts ; `validate-content` OK.

## 9. Risques & points ouverts

- **Diff structuré (S4)** : les messages du grader sont du texte non structuré ; `CheckService`
  dispose déjà de l'attendu et de l'obtenu en in-process → calculer le diff **dans `Piscine.App`** sans
  toucher au grader. Vérifier que cela couvre tous les types de cas (io, projet, mutation, git, reseau).
- **Auto-détection éditeur cross-platform** : VS Code (`code`) couvre la majorité ; Rider/VS plus
  variables → la surcharge Réglages est le filet. Repli dossier garanti.
- **`⌘K` dans le webview Photino** : le hotkey global passe par interop JS ; valider qu'il ne capture
  pas les frappes destinées au terminal embarqué.
- **Routage `/` → board** : vérifier la parité DevHost (Blazor Server) ↔ Photino (BlazorWebView), y
  compris l'indirection `InteractiveRenderSettings` (motif Blazor Hybrid) déjà en place.
- **Ampleur** : 9 sprints — chacun reste livrable indépendamment (board, ouvrir, rapport… ont de la
  valeur isolément), conforme au rythme 1 issue = 1 sprint.
