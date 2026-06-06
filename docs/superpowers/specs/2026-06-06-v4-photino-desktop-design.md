# Spec — v4 « Piscine Desktop » (application Photino Blazor)

> Design validé en brainstorming le 2026-06-06. Cible : remplacer l'**UX recrue console** par une
> **application de bureau Photino.Blazor**, sans toucher au moteur de correction ni au rendu git.
> À lire avec [HANDOFF](../HANDOFF.md) et la [roadmap v2/v3](../plans/2026-05-31-roadmap-v2-v3.md).

## 1. Contexte & motivation

La console est un mauvais médium pour lire des cours, afficher des diffs *attendu vs obtenu*,
naviguer entre exercices et visualiser la progression. v4 apporte une **vraie UX** tout en
**préservant les décisions figées** : retour éducatif jamais de note, **rendu via vrai git**,
correction par groupe séquentielle, distribution autonome.

**Point d'ancrage non négociable** : `grade-received <sha>` tourne **dans le hook git `post-receive`**
— headless, sans fenêtre. Le chemin de rendu officiel **reste donc le CLI**. v4 = *remplacer la
surface recrue*, **pas** supprimer le cœur headless.

## 2. Objectifs / Non-objectifs

**Objectifs**
- Une application de bureau (Photino.Blazor) = **unique UX recrue** : lecture de cours, navigation
  d'exercices, statut par exercice, **`check` instantané** richement rendu, **terminal git embarqué**
  avec **coaching** sur les erreurs.
- Le moteur (`Core`/`Grading`/`Git`) et le **CLI headless** restent inchangés et livrés.
- Une **pyramide de tests réelle** : services unitaires, composants (bUnit), **E2E navigateur
  (Playwright en CI)**, smoke Photino manuel par OS.

**Non-objectifs**
- Pas d'**éditeur de code embarqué** : la recrue code dans son IDE.
- Le terminal **ne masque pas** git : la recrue tape de **vraies** commandes git.
- Pas de site web *produit* : `Piscine.Web` est retiré en tant que produit (recyclé en harnais).
- Aucun changement du format de contenu ni des graders.

## 3. Décisions actées (brainstorming 2026-06-06)

| # | Décision | Conséquence |
|---|---|---|
| D1 | Photino.Blazor = unique UX recrue | Nouveau shell de bureau ; le moteur reste headless |
| D2 | L'app **complète** git (statut, init, **terminal embarqué**, coaching) | Pédagogie « vrai git » préservée |
| D3 | **Pas** d'éditeur embarqué | Périmètre borné ; IDE externe |
| D4 | L'app **remplace** `Piscine.Web` *en tant que produit* | Composants migrés vers une RCL |
| D5 | « Zéro pré-requis » **négociable** : setup webview unique toléré | Doc encadrant + checklist par OS |
| D6 | Garder un **harnais de test Blazor Server** (carcasse de `Piscine.Web`) | Débloque Playwright E2E + vérif locale |

## 4. Architecture

Sens des dépendances (les nouveautés en **gras**) :

```
Piscine.Core ──┬── Piscine.Grading ──┐
               ├── Piscine.Git ───────┤
               │                      ├── Piscine.Cli         (headless : grade-received, validate-content,
               │                      │                         package-content, new exercise) — INCHANGÉ, livré
               │                      │
               └── **Piscine.App** ───┤   (services SANS UI ni Photino : GitStatusService, CoachingService,
                                      │    CheckService, PtyService, modèles) → unit-testable
                                      │
                   **Piscine.Components** (RCL Razor : rendu cours Markdig, vues exo, panneau statut,
                          │                cartes d'indices, composant terminal xterm.js) → bUnit-testable
                          ├── **Piscine.Desktop** (Photino.Blazor) — shell LIVRÉ ; impls spécifiques OS
                          └── Piscine.Web         (Blazor Server) — **harnais test/dev, NON livré**
```

**Invariant clé** : toute la logique vit dans `Piscine.App` (sans dépendance Photino) et est consommée
**à l'identique** par le shell Photino *et* le harnais Blazor Server. C'est ce qui rend la même logique
serveur (PTY, git, FS) exécutable dans un navigateur pilotable par Playwright. Photino.Blazor et Blazor
Server partagent le **même modèle d'exécution .NET côté hôte** → le harnais est un analogue fidèle.

Projets de test : `Piscine.App.Tests` (xUnit), `Piscine.Components.Tests` (bUnit),
`Piscine.Web.E2E` (Playwright).

## 5. Le terminal embarqué + coaching (la pièce délicate)

Trois morceaux ; le troisième est là où la plupart des projets se plantent.

1. **Rendu** : `xterm.js` dans la webview, encapsulé par un composant RCL via JS interop.
2. **Backend PTY** : **`Pty.Net`** (ConPTY sous Windows, `forkpty` sous Unix). Lance le shell de la
   recrue, streame les octets vers xterm, gère le `resize`. **Risque technique n°1 → spike dédié en
   premier.** `PtyService` vit dans `Piscine.App` (côté hôte) → fonctionne aussi dans le harnais.
3. **Coaching — NE PAS parser le flux d'octets du terminal.** Deux signaux robustes :
   - **Shim `git`** placé **en tête de PATH** dans l'environnement du PTY : relaie vers le vrai git
     (MinGit sous Windows / git système ailleurs), capture l'`exit code`, et émet un **événement
     structuré** (`argv`, `exitCode`, `cwd`) vers `Piscine.App` via un canal local (named pipe /
     loopback). Pas de parsing de stdout.
   - **Inspection d'état du dépôt** (LibGit2Sharp) après chaque commande.
   Les règles de coaching se déclenchent alors **déterministes et testables** :

   | Situation détectée | Indice |
   |---|---|
   | `git commit` sans rien stagé | « Rien n'est indexé — `git add <fichiers>` d'abord. » |
   | Commité mais pas poussé | « Rendu non officiel tant que pas `git push origin main`. » |
   | Mauvaise branche pour l'exo | « Tu es sur `X`, l'exo attend `Y` (`git switch Y`). » |
   | Marqueurs de conflit dans les fichiers | « Conflit non résolu : `<<<<<<<` présent. » |
   | HEAD détaché | « HEAD détaché — reviens sur une branche. » |
   | Pas d'`origin` / `init` non fait | « Lance d'abord l'initialisation (bouton Init). » |
   | Commande git inconnue (typo) | « `git comit` ? Voulais-tu dire `commit` ? » |
   | Poussé mais `grade-received` KO | Renvoi vers le feedback éducatif rendu par l'app. |

**Échappatoire** : si le spike PTY se révèle pénible en cross-platform, repli sur « bouton *Ouvrir un
terminal ici* (shell OS) + coaching d'état ». Le moteur de coaching est donc conçu **agnostique au
shell** dès le départ — assurance anti-blocage.

## 6. Flux de données

- **`check` instantané (boucle rapide)** : sélection exo → `CheckService` appelle `Piscine.Grading`
  **in-process** → rendu diff *attendu vs obtenu* + indices + `course_ref`. **Sans git.** Remplace la
  sortie console de `piscine check`.
- **Rendu officiel (cœur inchangé)** : la recrue tape de vraies commandes git dans le terminal →
  `git push` → le hook `post-receive` lance `grade-received` (headless) → écrit le résultat éducatif →
  l'app le **surveille** et le rend richement. Rituel git et grader headless **préservés à l'identique**.
- **Boucle de coaching** : chaque commande git → événement shim + lecture d'état → MAJ panneau statut
  + cartes d'indices.

## 7. Stratégie de test (la pyramide)

| Couche | Outil | Où | Couvert par moi ? |
|---|---|---|---|
| Services (`Piscine.App`) : coaching, statut git, check, événements shim | xUnit | CI + local | ✅ écris & exécute |
| Composants RCL (vues, cartes, panneau) | **bUnit** (DOM simulé, sans navigateur) | CI + local | ✅ écris & exécute |
| E2E navigateur : navigation, feedback `check`, **terminal (PTY serveur → xterm)**, coaching | **Playwright** (Chromium headless) sur le **harnais Blazor Server** | CI + local | ✅ écris & exécute + **je vérifie via les outils preview** |
| Shell Photino natif : fenêtre, ConPTY/forkpty réel, lancement terminal OS | **Smoke manuel par OS** (checklist pré-release) | Local/manuel | ❌ je ne *vois* pas la fenêtre native (je peux confirmer qu'elle démarre via logs) |

Principe : **pousser toute la logique dans les services** ; garder le shell Photino **mince**. La seule
surface non automatisée est le shell spécifique Photino, petite et couverte par une checklist par OS.
Limite honnête : bUnit **mocke le JS interop** (donc pas le vrai xterm) → c'est le rôle du E2E Playwright
sur le harnais.

## 8. Distribution & packaging

- `release.yml` ajoute `Piscine.Desktop` **self-contained par RID** (binaires natifs Photino) à côté du
  CLI `piscine` **toujours livré** (le hook l'appelle), du `content/` et de MinGit (Windows).
- `Piscine.Web` **n'est pas empaqueté** (harnais test/dev uniquement).
- **Runtime webview = setup unique par poste**, documenté et ajouté à la checklist encadrant :
  WebView2 (Windows), `libwebkit2gtk` (Linux), intégré (macOS). Cohérent avec la gestion actuelle de
  git système sous Linux/macOS.
- Conséquences assumées : taille de zip et matrice de build en hausse ; perte de l'aperçu en ligne
  « zéro install » (puisque le site produit disparaît).

## 9. Backlog v4 (1 sprint = 1 issue, boucle SCRUM)

1. **Spike — shell Photino + RCL + harnais** : extraire les composants de `Piscine.Web` vers
   `Piscine.Components`, recycler `Piscine.Web` en hôte Blazor Server, squelette `Piscine.App`. Prouve
   le modèle bi-hôte + installe la pyramide de tests.
2. **Spike — terminal PTY embarqué** : `Pty.Net` + composant xterm.js + `PtyService` serveur (resize,
   débit), rendu dans le harnais + smoke Playwright. **Risque max, tôt.**
3. **Moteur statut git + coaching** : `GitStatusService` + `CoachingService` (règles déterministes) +
   shim `git` (événements structurés). xUnit + E2E.
4. **`check` instantané in-process** : `CheckService` + composant de rendu du feedback (diff/indices/réf).
5. **Navigation d'exercices + progression** (UI).
6. **Lecteur de cours/sujets** (RCL migrée) + parité mode sombre.
7. **Init/setup in-app** : enrobe `piscine init` (workspace + dépôt bare + hook).
8. **Surveillance du résultat de push** + rendu riche de `grade-received`.
9. **Packaging/release** : Photino par-RID dans `release.yml`, docs setup webview, exclusion confirmée
   de `Piscine.Web` + checklist smoke manuelle par OS.
10. **Docs** : réécriture recrue + encadrant pour le flux desktop ; MAJ `Curriculum.md` / HANDOFF.

## 10. Risques & spikes

- **PTY cross-platform** = risque dominant → spike #2 en premier ; coaching agnostique au shell en
  assurance ; repli « terminal OS » documenté.
- **Disponibilité webview** : même « négociable », c'est une charge de support → docs + checklist ; le
  spike #1 valide la disponibilité réelle sur Windows (dont éditions **N**), Linux, macOS.
- **Coaching par parsing de stdout** = piège → écarté au profit du shim + état dépôt.
- **UI peu auto-testable** → logique dans les services, shell mince, harnais Blazor Server.

## 11. Hors périmètre v4

Éditeur de code embarqué ; LSP/autocomplétion ; remplacement du CLI headless ; enhancements V3 ouverts
(#17 notation live git, #19 harnais web) — orthogonaux, traités séparément.
