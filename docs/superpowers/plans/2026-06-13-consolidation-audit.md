# Plan d'exécution — Itération de consolidation (audit factuel)

> **Pour worker agentique :** SOUS-SKILL REQUISE : `superpowers:subagent-driven-development`
> (recommandé) ou `superpowers:executing-plans` pour exécuter tâche par tâche. Cases `- [ ]`.

**Goal :** Produire `docs/superpowers/audits/2026-06-13-consolidation-audit.md` — un rapport de
constats priorisé, **factuel/outillé**, sur 5 axes (CI, couverture, SOLID/KISS, docs GitHub, backlog),
sans modifier code/CI/contenu.

**Architecture :** Lecture seule + mesures locales transitoires. Une **tâche par axe** collecte des
preuves reproductibles (commande + sortie capturée) et rédige sa section ; une tâche finale synthétise
le résumé exécutif + table d'actions priorisée. La sortie est **un seul fichier** Markdown, construit
incrémentalement (1 commit par section).

**Tech Stack :** .NET 10 (`dotnet test --collect`), ReportGenerator (outil local), MCP Roslyn
`dotnet-claude-kit` (antipatterns / code mort / cycles / graphe de deps), `gh` (durées CI), `git`.

**Spec source :** `docs/superpowers/specs/2026-06-13-consolidation-audit-design.md`.

**Note méthode (pas de TDD) :** déliverable = document. « Vérifié » pour une tâche = la preuve est
reproductible (commande exacte + sortie collée) et chaque constat porte `Pn` + effort + reco.

---

## Préliminaire (avant Tâche 1)

- [ ] **P.1 — Confirmer la branche de travail**

Run : `git branch --show-current`
Attendu : `consolidation/audit` (créée en brainstorming, porte déjà le commit de la spec `52bb6f5`).

- [ ] **P.2 — S'assurer que les artefacts transitoires ne seront pas versionnés**

Run : `git check-ignore -v artifacts/ TestResults/ .tools/ ; echo "exit=$?"`
Si un de ces chemins n'est PAS ignoré, ajouter une entrée locale **non commitée** via
`.git/info/exclude` (ne PAS modifier `.gitignore` versionné — hors périmètre) :
```bash
printf '%s\n' 'artifacts/' 'TestResults/' '.tools/' >> .git/info/exclude
```
Attendu : avant chaque commit, `git status --porcelain` ne montre que le fichier d'audit.

---

## Tâche 1 : Axe CI — mesures + analyse

**Files :**
- Create : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (squelette + section CI)

- [ ] **Step 1 : Relever les durées réelles des derniers runs CI**

```bash
gh run list --workflow=ci.yml --branch main --limit 15 \
  --json databaseId,displayTitle,event,status,conclusion,createdAt,updatedAt \
  > artifacts/ci-runs.json
cat artifacts/ci-runs.json
# Durée par run = updatedAt - createdAt ; relever aussi un run récent par job :
gh run view "$(gh run list --workflow=ci.yml --limit 1 --json databaseId -q '.[0].databaseId')" \
  --json jobs -q '.jobs[] | {name, startedAt, completedAt, conclusion}'
```
Attendu : durées par run + par job (build-test / appimage-offline-dryrun / windows-installer-dryrun).

- [ ] **Step 2 : Lister les commits récents pour estimer la part de PR docs-only**

```bash
git log --oneline -30 --pretty='%h %s' | grep -ci '^.* docs' || true
git log -30 --name-only --pretty='%h %s' | grep -E '^(src/|content/|.github/)' | head
```
Objectif : étayer le constat « les dry-runs lourds tournent aussi sur des PR purement docs ».

- [ ] **Step 3 : Rédiger la section CI du rapport**

Créer le fichier avec : en-tête + table d'actions (vide, remplie en Tâche 6) + **section CI**.
Constats attendus, chacun avec `Pn`/effort/preuve :
- Pas de cache NuGet (`actions/setup-dotnet` `cache: true` ou `actions/cache` sur `~/.nuget/packages`).
- Pas de `concurrency:` (runs redondants non annulés sur pushes successifs).
- Dry-runs lourds (AppImage offline + installeur Windows + publish cross-RID) sans **filtre de chemins**
  → tournent sur PR docs-only. Proposer `paths:`/`paths-ignore:` ou un job-gate `git diff`.
- Re-restore/-publish redondants entre `build-test` et le step packaging.
Chiffrer le gain (minutes/run × fréquence).

- [ ] **Step 4 : Vérifier la reproductibilité**

Run : relire la section ; chaque constat cite une commande/un extrait de `ci.yml`/`release.yml` ou une
sortie `gh`. Pas d'affirmation sans preuve.

- [ ] **Step 5 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): axe CI — constats + mesures\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 2 : Axe couverture — mesure réelle

**Files :**
- Modify : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (ajout section Couverture)

- [ ] **Step 1 : Installer ReportGenerator en outil local (sans polluer le repo)**

```bash
dotnet tool install dotnet-reportgenerator-globaltool --tool-path .tools 2>&1 | tail -2 || true
.tools/reportgenerator --help > /dev/null && echo "reportgenerator OK"
```
Attendu : binaire dispo sous `.tools/` (ignoré). En cas d'échec réseau, noter le constat et basculer
sur la lecture de `coverage.cobertura.xml` brute (le `<coverage line-rate=...>` racine donne déjà le %).

- [ ] **Step 2 : Lancer les tests avec collecte de couverture**

```bash
dotnet test Piscine.slnx -c Release --collect:"XPlat Code Coverage" \
  --results-directory artifacts/coverage --verbosity quiet
```
Attendu : 267 tests verts (E2E Playwright skippés sans navigateur), 1 `coverage.cobertura.xml` par
projet de test sous `artifacts/coverage/<guid>/`.

- [ ] **Step 3 : Agréger en résumé lisible**

```bash
.tools/reportgenerator -reports:"artifacts/coverage/**/coverage.cobertura.xml" \
  -targetdir:artifacts/coverage/report -reporttypes:"TextSummary;MarkdownSummaryGithub"
cat artifacts/coverage/report/Summary.txt
```
Attendu : % lignes + branches **global et par assembly** (`Piscine.Core`, `Piscine.Grading`,
`Piscine.Git`, `Piscine.App`, `Piscine.Components`, …).

- [ ] **Step 4 : Rédiger la section Couverture**

Tableau % par assembly + lecture qualitative : **prioriser le code critique sous-testé** (graders,
`grade-received`, coaching) plutôt que le chiffre global. Noter que `Piscine.Cli`/`Piscine.DevHost`/
`Piscine.Desktop` (surface UI/hôte) tirent le global vers le bas mais sont couverts par E2E/smoke.
Chaque manque = `Pn`/effort. Pas de seuil imposé (proposition seulement).

- [ ] **Step 5 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): axe couverture — %% réels par assembly\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 3 : Axe SOLID / KISS — analyse Roslyn + lecture ciblée

**Files :**
- Modify : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (ajout section SOLID/KISS)

- [ ] **Step 1 : Charger les outils MCP Roslyn**

```
ToolSearch query: "select:mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__detect_antipatterns,mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__find_dead_code,mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__detect_circular_dependencies,mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__get_dependency_graph,mcp__plugin_dotnet-claude-kit_cwm-roslyn-navigator__get_diagnostics"
```
Attendu : schémas chargés (sinon, repli sur lecture manuelle + `dotnet build` warnings).

- [ ] **Step 2 : Exécuter l'analyse statique sur la solution**

Appeler, sur `Piscine.slnx` : `detect_antipatterns`, `find_dead_code`, `detect_circular_dependencies`,
`get_dependency_graph`. Capturer les sorties (résumé chiffré : nb antipatterns par catégorie, code mort,
cycles éventuels, sens des dépendances entre projets).

- [ ] **Step 3 : Lecture manuelle des plus gros fichiers**

Read : `src/Piscine.Grading/ContentValidator.cs` (359), `src/Piscine.Cli/Program.cs` (265),
`src/Piscine.Grading/GitGrader.cs` (255), `src/Piscine.Git/GradeReceivedCommand.cs` (236),
`src/Piscine.Grading/ProjectGrader.cs` (216), `src/Piscine.App/Push/ProgressFileWatcher.cs` (216).
Juger SRP / couplage / duplication réels.

- [ ] **Step 4 : Rédiger la section SOLID/KISS**

Pour chaque constat : citer fichier:ligne + sortie Roslyn ; `Pn`/effort. **Garde KISS explicite** :
ne PAS recommander de pattern là où un `switch`/une procédure linéaire suffit ; signaler aussi toute
**sur-ingénierie existante** comme un constat. Distinguer « dérive réelle » de « préférence stylistique »
(ces dernières → P3 ou écartées).

- [ ] **Step 5 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): axe SOLID/KISS — Roslyn + lecture ciblée\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 4 : Axe documentation GitHub — vérification d'exactitude

**Files :**
- Modify : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (ajout section Docs)

- [ ] **Step 1 : Inventorier la doc**

```bash
git ls-files 'README*' 'CHANGELOG*' 'docs/**/*.md' | sort
```
Read : `README.md`, `CHANGELOG.md`, `docs/wiki/*.md`, `docs/mise-en-oeuvre.md`, `docs/deploiement.md`.

- [ ] **Step 2 : Confronter à l'état réel**

Vérifier : numéros de version (`v3.0.0`), liste des projets `src/` (9 projets — Components/App/Desktop/
DevHost/GitShim présents ?), nb de tests cité (267), nb de graders (7), `Piscine.Web`→`Piscine.DevHost`,
webkit2gtk-**4.0** (pas 4.1) cohérent partout, macOS abandonné. Liens internes/externes morts :
```bash
grep -rhoE '\]\(([^)]+)\)' README.md CHANGELOG.md docs/*.md docs/wiki/*.md | sort -u | head -60
```
(vérifier les chemins relatifs existent ; signaler les URLs douteuses sans les visiter en masse.)

- [ ] **Step 3 : Constat « dépôt public → corrigés exposés »**

Documenter : repo public ⇒ `content/**/solution/*` visible (≈120 dossiers) ; la promesse « la recrue ne
voit jamais le corrigé » ne tient plus que pour le **paquet livré** (`package-content` les exclut).
Lister les options (sans trancher) : submodule privé, dépôt corrigés séparé, branche orpheline,
injection au build, ou accepter formellement. `Pn`/effort.

- [ ] **Step 4 : Rédiger la section Docs**

Constats d'exactitude (chaque écart : fichier + ligne + ce qui est faux) + le constat exposition. `Pn`/effort.

- [ ] **Step 5 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): axe documentation GitHub — exactitude + exposition corrigés\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 5 : Axe backlog / issues — réconciliation

**Files :**
- Modify : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (ajout section Backlog)

- [ ] **Step 1 : État GitHub**

```bash
gh issue list --state open --limit 50
gh issue list --state all --limit 100 --json number,title,state,milestone -q '.[] | "\(.number) [\(.state)] \(.title)"' | tail -20
gh api repos/:owner/:repo/milestones --jq '.[] | "\(.title) open=\(.open_issues) closed=\(.closed_issues)"'
```
Attendu : confirmer 0 ouverte, 3 milestones clos.

- [ ] **Step 2 : Extraire les suivis connus non tracés du HANDOFF**

Read : `docs/superpowers/HANDOFF.md` (sections « Suivis (post-v4) » a–e ; « Enhancements de suivi » #19 ;
#3 serveur HTTP/exo M22 ; « PROCHAINE TÂCHE = ENRICHISSEMENT » bonus M00–M18/M21/M23).

- [ ] **Step 3 : Rédiger la section Backlog + backlog proposé**

Tableau : suivi connu → est-il tracé en issue ? → reco (créer / classer / abandonner). Proposer une
liste d'issues à (re)créer avec milestone/label suggérés. **Aucune création d'issue ici** (hors périmètre).
`Pn`/effort par item.

- [ ] **Step 4 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): axe backlog — réconciliation suivis non tracés\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 6 : Synthèse — résumé exécutif + table d'actions priorisée

**Files :**
- Modify : `docs/superpowers/audits/2026-06-13-consolidation-audit.md` (en-tête)

- [ ] **Step 1 : Compiler tous les constats**

Relire les 5 sections ; lister chaque constat avec son `Pn`/effort.

- [ ] **Step 2 : Rédiger le résumé exécutif + la table priorisée**

En tête du document : 3-5 phrases de synthèse (santé globale du projet) + **table** triée par sévérité
puis effort : `ID | Axe | Constat | Sévérité | Effort | Reco`. Proposer un **ordre de traitement**
(quick wins P1/S d'abord). Rappeler que rien n'est encore exécuté.

- [ ] **Step 3 : Vérifier la cohérence**

Chaque ligne de la table renvoie à une section détaillée ; aucun constat orphelin ; sévérités cohérentes.

- [ ] **Step 4 : Commit**

```bash
git add docs/superpowers/audits/2026-06-13-consolidation-audit.md
git commit -m "$(printf 'docs(audit): synthèse — résumé exécutif + table d''actions priorisée\n\nCo-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>')"
```

---

## Tâche 7 : Auto-revue, livraison

- [ ] **Step 1 : Auto-revue du rapport**

Relire le rapport entier : pas de placeholder, chaque constat a preuve + `Pn` + effort + reco, table
cohérente avec les sections, périmètre respecté (aucun changement code/CI/contenu, aucune issue créée).

- [ ] **Step 2 : Vérifier qu'aucun artefact transitoire n'est versionné**

```bash
git status --porcelain
```
Attendu : seul le fichier d'audit (et la spec/plan déjà commités) ; pas de `artifacts/`, `TestResults/`, `.tools/`.

- [ ] **Step 3 : Pousser la branche + ouvrir la PR (DEMANDER LE GO au proprio d'abord)**

Action **outward-facing** sur repo public → confirmer avec le proprio avant :
```bash
git push -u origin consolidation/audit
gh pr create --base main --title "Audit de consolidation (CI, couverture, SOLID/KISS, docs, backlog)" \
  --body "Rapport factuel priorisé. Aucun changement de code/CI/contenu ; backlog proposé, pas créé. Voir docs/superpowers/audits/2026-06-13-consolidation-audit.md"
```

- [ ] **Step 4 : Consigner dans le HANDOFF (optionnel, après merge)**

Ajouter une ligne « itération de consolidation : audit produit, voir audits/2026-06-13-... » si le proprio
veut tracer l'itération.

---

## Auto-revue du plan (faite)

- **Couverture spec :** les 5 axes de la spec → Tâches 1–5 ; sortie unique + table priorisée → Tâche 6 ;
  périmètre « aucun changement / pas d'issue / pas de tag » rappelé dans Tâches 5/7. ✓
- **Placeholders :** aucun « TBD/TODO » ; chaque tâche a des commandes exactes. (Les *valeurs* chiffrées
  — % couverture, durées CI — sont produites à l'exécution, par conception d'un audit.) ✓
- **Cohérence :** nom de fichier d'audit identique partout ; branche `consolidation/audit` cohérente ;
  trailer commit `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (forme du repo). ✓
