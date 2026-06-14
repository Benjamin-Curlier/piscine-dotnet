# Spec — Harnais de test agentique (QA piloté par Claude) + boucle d'amélioration

> Brainstorming validé le 2026-06-14. Objectif : donner à Claude un outillage pour **piloter l'app comme
> une personne** (naviguer, capturer des écrans, agir), **juger la qualité** contre une rubrique, puis
> **améliorer l'UI de façon autonome jusqu'à une barre**, avec un rapport avant/après.

## 1. Contexte

- L'app de bureau Photino partage son UI (RCL `Piscine.Components`) avec un hôte web **`Piscine.DevHost`**
  (ASP.NET, Blazor) utilisé pour le dev et les tests E2E.
- Les tests E2E (`tests/Piscine.DevHost.E2E`) démarrent déjà le DevHost via
  `dotnet run --project src/Piscine.DevHost --urls http://localhost:PORT` avec
  `PISCINE_CONTENT` / `PISCINE_WORKSPACE` / `PISCINE_HOME` pointant un dossier temporaire **dont le
  `progress.json` est planté** (`Progress` + `ProgressStore`), puis pilotent Chromium via Playwright en
  ciblant des sélecteurs **`data-testid`**.
- Un **Playwright MCP** est connecté dans la session Claude (`browser_navigate`, `browser_snapshot`,
  `browser_take_screenshot`, `browser_click`, `browser_resize`, `browser_evaluate`, …) — il fournit
  déjà naviguer/capturer/cliquer.
- Limite découverte récemment : la vérification interactive du chrome de fenêtre a échoué (computer-use
  natif indisponible). Le DevHost en navigateur, lui, est pilotable sans friction.
- Dette connexe déjà repérée : l'overlay d'onboarding (S7) intercepte les clics quand le workspace n'est
  pas initialisé → casse des E2E en local. Un seed déterministe résout ce non-déterminisme.

## 2. Objectifs / Non-objectifs

**Objectifs**
- **États déterministes** : lancer le DevHost dans des **profils** nommés qui plantent un état connu
  (onboarding, progression mixte, échec de check, push récent, presque terminé).
- **Pilotage** via le **Playwright MCP existant** (pas de nouveau code de pilotage en Phase 1).
- **Skill QA-and-refine** : un workflow répétable qui parcourt tous les écrans × états × thèmes ×
  largeurs, capture, **note contre une rubrique**, **corrige de façon autonome** dans les jetons du
  design existant, **réévalue jusqu'à une barre**, et produit un **rapport avant/après**.
- **Spot-check natif** mince pour les surfaces propres à Photino (chrome de fenêtre, terminal embarqué).
- **Phasing** : Phase 1 = seed + Playwright MCP + skill ; **Phase 2 = MCP custom seulement si la Phase 1
  est trop fragile.**

**Non-objectifs (YAGNI)**
- Pas de serveur MCP custom en Phase 1.
- Pas de refonte visuelle / nouveau design — uniquement des correctifs **dans les jetons existants**
  (`piscine.css`, CSS de composants).
- Aucune modification du **moteur / `Piscine.Cli` / `Piscine.Grading` / `Piscine.Git` / `grade-received` /
  logique `release.yml`** (gelés).
- Pas de tests de charge/perf ; pas de cross-navigateur (Chromium suffit).

## 3. Architecture & composants

### 3.1 Launcher + profils de seed (la fondation manquante)
- **`scripts/devhost-qa.ps1`** (+ variante bash `scripts/devhost-qa.sh` pour la parité Linux/CI) :
  `devhost-qa --profile <nom> --port <p>` →
  1. crée un `PISCINE_HOME` temporaire (workspace + `.state`),
  2. **plante l'état du profil** (progress.json via le format `Progress`/`ProgressStore` ; éventuels
     livrables de workspace ; `last-push-result.json` pour le profil push),
  3. lance `dotnet run --project src/Piscine.DevHost --urls http://localhost:<p>` avec les env vars,
  4. attend que le serveur réponde, imprime l'URL, et nettoie le temp à l'arrêt.
- **Profils** (un état distinct et utile chacun) :
  | Profil | État planté | Ce qu'il révèle |
  |---|---|---|
  | `fresh` | workspace NON initialisé | onboarding 1er lancement (overlay) |
  | `mixed` | initialisé, progression variée (Fait/EnCours/ARevoir sur plusieurs modules) | tableau de bord, board, pastilles de nav |
  | `exo-fail` | un exo avec dernier check en échec | diff structuré coloré (`CheckFeedback`) |
  | `exo-pass` | un exo réussi | états « réussi » |
  | `push-result` | `last-push-result.json` récent présent | toast de push + `/resultat` |
  | `done` | quasi tout en Fait | `/rapport` significatif, progression ~complète |
- **Déterminisme** : chaque profil produit le MÊME rendu à chaque lancement (pas d'horloge/aléa visibles).
- Réutiliser le code de seed des E2E existants (ne pas dupliquer la logique `Progress`).
- **Couverture `data-testid`** : auditer les écrans/contrôles ciblés par la QA et **ajouter les
  `data-testid` manquants** (sélection sémantique robuste). Édition UI minimale et additive (RCL).

### 3.2 Pilotage = Playwright MCP existant
- Claude pilote le DevHost planté via les outils `mcp__…playwright…__browser_*` : `navigate`,
  `snapshot` (arbre a11y), `take_screenshot`, `click`, `resize`, `evaluate` (erreurs console).
- **Aucun nouveau code de pilotage en Phase 1.** (Phase 2 : envelopper les actions sémantiques éprouvées
  dans un MCP custom seulement si les appels bruts s'avèrent trop fragiles/verbeux.)

### 3.3 Skill « QA-and-refine » (workflow + rubrique + boucle)
- Un **skill de repo** (sous `.claude/skills/` ou `docs/superpowers/`) encodant la boucle autonome :
  1. Pour chaque **profil × route × thème (clair/sombre) × largeur (1280/1024/768/420)** : naviguer,
     capturer l'écran, capturer les erreurs console, snapshot a11y.
  2. **Noter contre la rubrique** (§3.4). Consigner chaque constat avec preuve (capture).
  3. **Corriger** les constats dans les jetons du design existant (pas de refonte), en gardant
     build/tests/smoke verts.
  4. **Recapturer**, comparer avant/après, **répéter jusqu'à ce que la barre passe** ou la limite
     d'itérations (≤ 3 par zone).
  5. Produire un **rapport avant/après** (galerie de captures + constats résolus / différés).

### 3.4 Rubrique / barre de qualité (objective autant que possible)
- **Zéro erreur console** / aucune Blazor error UI sur aucun écran.
- **Pas de débordement/rognage** ni scrollbars inattendues aux largeurs cibles.
- **Parité mode sombre** : chaque écran lisible et sur jetons dans les deux thèmes (contraste **AA**).
- **`:focus-visible`** sur tous les éléments interactifs ; navigable au clavier (skip-link, ordre de tab).
- **États vide / chargement / erreur** présents et stylés (pas bruts/blancs).
- **Cohérence visuelle** : espacements/typo/couleurs via variables CSS (pas de valeurs one-off).
- **Flux** : parcours principal (init → 1er exo → check → résultat de push → rapport) sans impasse.

### 3.5 Garde-fous de la correction autonome
- Modifs **limitées à `piscine.css` / CSS de composants** (jetons) ; **pas** de nouvelle dépendance ;
  **pas** de changement moteur/CLI/grade-received/`release.yml`.
- **Itérations bornées** par zone (≤ 3) pour éviter l'emballement.
- Chaque zone corrigée = **commit révisable avec captures avant/après** ; PR(s) de taille raisonnable
  (pas un diff géant non révisable).
- **Drapeau plutôt qu'imposition** : tout constat « subjectif » (choix de goût, pas « objectivement
  cassé ») est **signalé dans le rapport**, pas corrigé d'autorité.

### 3.6 Spot-check natif (surfaces propres à Photino, ~5 %)
- Chrome de fenêtre chromeless (drag/réduire/agrandir/fermer) + terminal embarqué : via la sonde de
  rendu (étendue si besoin) + computer-use **quand l'accès est accordé**, sinon **checklist manuelle**
  courte. Volontairement mince.

## 4. Flux de données

`profil de seed → DevHost dans cet état → Playwright MCP pilote → captures + console + a11y → rubrique →
correctifs jetons (RCL/CSS) → rebuild → re-pilotage → rapport avant/après`.

## 5. Tests & vérification du harnais lui-même

- **Smoke de profil** : pour chaque profil, un test léger (ou une étape de skill) confirme que le DevHost
  démarre dans cet état (le `data-testid` emblématique du profil est rendu — ex. `fresh` → overlay
  onboarding visible ; `mixed` → pastilles de statut ; `exo-fail` → diff).
- Les **PR de refonte** gardent verts : bUnit (`Piscine.Components.Tests`), E2E (`Piscine.DevHost.E2E`),
  smoke de rendu Photino (gate local), build Release **0 warning**.
- Le launcher n'introduit **aucun** code applicatif (scaffolding de test/scripts uniquement) ; les
  ajouts `data-testid` sont additifs et couverts par bUnit si pertinent.

## 6. Scope / phasing

- **Phase 1 (cet épic)** : launcher `devhost-qa` + profils de seed + couverture `data-testid` + smoke de
  profils + skill QA-and-refine + **exécuter la boucle autonome** → PR(s) de refonte + rapport avant/après.
  Absorbe la dette **overlay d'onboarding** (le profil `fresh` l'exerce ; correctif inclus si la rubrique
  l'exige, ou délégué). Spot-check natif mince.
- **Phase 2 (seulement si la Phase 1 est trop fragile)** : serveur MCP custom (`seed_state(profil)`,
  `goto(route|exo)`, `run_check()`, `read_progress()`, `screenshot()`, `list_routes()`).

## 7. Risques

- **Dérive de goût autonome** → rubrique objective + correctifs jetons-only + revue avant/après finale +
  signaler-sans-imposer le subjectif.
- **Emballement de boucle / diff géant** → itérations bornées + PR(s) scoppées.
- **DevHost ≠ Photino exactement** : visuels identiques mais plomberie des îlots interactifs différente
  (DevHost = `InteractiveServer` sur circuit réel ; Photino = in-process) → spot-check natif couvre le gap.
- **Fragilité du Playwright MCP** (sélecteurs/temps) → atténuée par les `data-testid` et le seed
  déterministe ; déclenche la Phase 2 si récurrent.

## 8. Invariants

Outillage de test/scripts + skill + `data-testid` additifs uniquement. UI → `Piscine.Components` ; logique
éventuelle → `Piscine.App`. Moteur/CLI/`grade-received`/`release.yml` (logique) intacts. Commits
conventionnels FR + trailer `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` ;
commit ≠ push.
