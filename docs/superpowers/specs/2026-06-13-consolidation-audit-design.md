# Conception — Itération de consolidation (audit factuel)

> Date : 2026-06-13. Issu d'un brainstorming. **Deliverable de cette itération = UN
> document de constats priorisé**, pas de modification de code/CI. Décide ensuite quoi traiter.

## Objectif

Faire une revue transversale de l'état du projet (post-`v3.0.0`, milestones #1/#2/#3 clos,
0 issue ouverte, 267 tests verts) sur **cinq axes** demandés par le proprio :

1. **CI** — l'optimiser.
2. **Couverture de tests** — la **vérifier** (mesurer, pas estimer).
3. **SOLID / KISS** — repérer les vraies dérives de responsabilité/couplage, sans sur-ingénierie.
4. **Documentation GitHub** — vérifier son exactitude (README, CHANGELOG, Wiki, HANDOFF).
5. **Backlog / issues** — réconcilier les suivis connus non tracés avec le backlog GitHub vide.

## Décisions actées en brainstorming

- **Deliverable** : audit + document de constats d'abord. **Aucun changement de code/CI**, aucune
  création d'issue, aucun tag/release dans cette itération.
- **Profondeur** : **factuelle / outillée**. On exécute les outils plutôt que d'estimer
  (couverture réelle, analyse Roslyn, durées CI).
- **Structure** : axe par axe (couvre les 5 demandes), **scoring sévérité + effort** sur chaque
  constat, et **liste d'actions priorisée** en tête (backlog proposé prêt à reprendre).

## Sortie

- **Un seul fichier** : `docs/superpowers/audits/2026-06-13-consolidation-audit.md`
  (nouveau dossier `audits/` — c'est un rapport, ni spec, ni plan, ni retex).
- En-tête : **résumé exécutif + table d'actions priorisée**.
- Puis : une **section par axe** avec preuves (chiffres, extraits, sorties d'outils).

## Modèle de sévérité / effort

| Sévérité | Sens |
|---|---|
| **P0** | Correction / sécurité / perte de données / casse de la promesse pédagogique |
| **P1** | Forte valeur, à faire bientôt |
| **P2** | Utile, non urgent |
| **P3** | Cosmétique |

Effort : **S** (≤ ½ j) · **M** (~1–2 j) · **L** (> 2 j). Chaque constat = `Pn` + effort + recommandation.

## Méthode de collecte des preuves (par axe)

### 1. CI
- Lire `ci.yml` / `release.yml` (fait) ; relever les durées réelles via `gh run list` / `gh run view`.
- Évaluer : cache NuGet absent (`setup-dotnet` cache / `actions/cache`), `concurrency` (annulation des
  runs redondants) absent, **filtres de chemins** pour ne pas lancer les dry-runs lourds (AppImage offline
  + installeur Windows + publish cross-RID) sur les PR **docs-only** (fréquentes dans la boucle SCRUM),
  re-`restore`/`publish` redondants. Chiffrer le gain de temps/coût estimé.

### 2. Couverture de tests
- Exécuter réellement `dotnet test Piscine.slnx -c Release --collect:"XPlat Code Coverage"`
  (+ ReportGenerator, installé en outil local) → **% lignes/branches réel par projet**.
- Pointer le code **critique sous-testé** en priorité (graders, `grade-received`, coaching git) plutôt
  que le simple chiffre global. Noter les E2E Playwright qui skippent sans navigateur (n'inflent pas le chiffre).

### 3. SOLID / KISS
- Outils Roslyn (MCP `dotnet-claude-kit`) : antipatterns, code mort, graphe de dépendances, cycles.
- Lecture manuelle des plus gros fichiers : `ContentValidator.cs` (359), `Cli/Program.cs` (265),
  `GitGrader.cs` (255), `GradeReceivedCommand.cs` (236), `ProjectGrader.cs` (216), `ProgressFileWatcher.cs` (216).
- **Ne signaler que les vraies dérives** (SRP, couplage, duplication). **KISS = ne PAS recommander de
  remplacer un `switch` qui marche par un pattern Command.** La sur-ingénierie est elle-même un constat.

### 4. Documentation GitHub
- Vérifier l'exactitude vs l'état réel : `README`, `CHANGELOG`, Wiki (`docs/wiki/`), `docs/*.md`, HANDOFF.
- Liens cassés, sections périmées, incohérences de version.
- **Constat explicite** : dépôt **public** → `solution/` (corrigés) exposés publiquement, ce qui affaiblit
  la promesse « la recrue ne voit jamais les corrigés » (encore tenue seulement pour le paquet livré).

### 5. Backlog / issues
- Réconcilier les **suivis connus non tracés** (HANDOFF « Suivis post-v4 » a–e ; **#19** différé ; serveur
  **HTTP** / exo **M22** ; enrichissement **bonus M00–M18/M21/M23**) avec le **backlog GitHub vide**.
- Proposer une liste d'issues à (re)créer pour que « 0 issue ouverte » cesse de masquer du travail connu.
  **Proposition seulement — pas de création d'issue dans cette itération.**

## Hors périmètre (explicite)

- Aucune édition de `src/`, `content/`, `.github/`, docs (hormis créer le rapport d'audit).
- Aucune création d'issue GitHub, aucun `git tag`, aucune release.
- Mesures locales transitoires uniquement (la couverture écrit dans `TestResults/`, gitignoré) ; le seul
  artefact versionné est le document d'audit.

## Étape suivante

Après validation du proprio sur ce document de conception → `writing-plans` pour le plan d'exécution
de l'audit (ordre des axes, mise en place ReportGenerator, commandes Roslyn), puis exécution et
production du rapport `docs/superpowers/audits/2026-06-13-consolidation-audit.md`.
