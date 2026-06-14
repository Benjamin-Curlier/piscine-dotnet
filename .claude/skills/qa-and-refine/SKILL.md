---
name: qa-and-refine
description: Piloter le DevHost (via le Playwright MCP) dans des états seedés, juger la qualité contre une rubrique, corriger dans les jetons existants, réévaluer jusqu'à une barre, produire un rapport avant/après. Déclencheurs : "passe QA", "qa-and-refine", "juge la qualité de l'app", "améliore l'UI".
---

# QA-and-refine (boucle d'amélioration pilotée)

Boucle autonome : **lancer un état seedé → piloter via le Playwright MCP → noter contre la
rubrique → corriger dans les jetons existants → réévaluer → rapport avant/après.**

## Lancer un état (harnais)

```
pwsh scripts/devhost-qa.ps1 -Profile <fresh|mixed|exo-fail|exo-pass|push-result|done> -Port 5240
# ou, Linux/macOS :
scripts/devhost-qa.sh <profil> 5240
```

Le launcher crée un `PISCINE_HOME` temporaire isolé, fixe `PISCINE_QA_PROFILE` et démarre le
DevHost ; le hook QA seede l'état au démarrage via les types réels du moteur. Attendre la ligne
`url=http://localhost:5240/`, puis piloter via le Playwright MCP. Ctrl-C nettoie le temp.

### Profils
| Profil | État | Ce qu'il révèle |
|---|---|---|
| `fresh` | workspace NON initialisé | overlay onboarding (`data-testid="onboarding"`) |
| `mixed` | initialisé, progression variée | tableau de bord, board, pastilles (`status-dot` : `PousseNote`/`EnCours`/`ARevoir`/`NonCommence`) |
| `exo-fail` | un exo « à revoir » + artefact riche d'échec | diff coloré (`CheckFeedback`) |
| `exo-pass` | un exo réussi | états « réussi » |
| `push-result` | progression + `last-push-result.json` récent | toast de push + `/resultat` |
| `done` | tout en Reussi | `/rapport` significatif, progression ~complète |

### Note sur le toast / `/resultat` (push-result, exo-fail)
Le `ToastHost` et `/resultat` s'abonnent à `IPushResultWatcher.ResultReceived`, qui ne publie un
verdict que sur un **changement** de `progress.json` postérieur au démarrage du watcher (snapshot de
base au 1ᵉʳ rendu). Le seeder écrit `last-push-result.json` + `progress.json` sur disque, mais pour
**déclencher** le toast/diff riche, re-toucher `progress.json` APRÈS chargement de la page :

```
browser_evaluate(() => fetch('/')) ; puis re-sauver progress.json (même contenu suffit)
```

Concrètement, depuis le terminal du launcher, ouvrir `$PISCINE_HOME/.state/progress.json` et le
ré-enregistrer (ou ajouter un exo) une fois la page `/resultat` ouverte → le FSW (debounce 250 ms)
publie le delta → toast + diff riche apparaissent. Le tableau de bord et `/rapport`, eux, lisent
`progress.json` directement et rendent l'état seedé **dès le chargement** (pas de re-touch requis).

## Matrice de capture
Pour chaque **profil** pertinent × **route** (`/`, `/cours`, `/module/{m}/{e}`, `/check`,
`/resultat`, `/rapport`, `/reglages`, palette ⌘K) × **thème** (clair, sombre) × **largeur**
(1280, 1024, 768, 420) :
1. `browser_navigate` (route) ; basculer le thème (bouton `#theme-toggle`) ; `browser_resize` (largeur).
2. `browser_take_screenshot` (nommer : `<profil>-<route>-<theme>-<largeur>.png`).
3. `browser_evaluate` → relever `console.error` / avertissements et l'éventuel `#blazor-error-ui` visible.
4. `browser_snapshot` → vérifier l'arbre a11y (focus, rôles, libellés).

### Ancres `data-testid` utiles
`dashboard`, `board-percent`, `board-module`, `status-dot`/`status-badge`, `module-grid`,
`module-page`, `exercise-page`, `exercise-actions`, `exo-select`/`run-check`, `check-verdict`,
`diff-block`/`diff-expected`/`diff-actual`, `check-hint`/`check-course-ref`, `push-empty`/`push-entry`,
`push-toast`/`toast-entry`, `report`/`report-module-table`, `settings`/`settings-theme`,
`command-palette`/`command-palette-input`, `onboarding`/`onboarding-welcome`.

## Rubrique (barre)
- Zéro erreur console / aucune Blazor error UI sur aucun écran.
- Pas de débordement/rognage ni scrollbar inattendue aux largeurs cibles.
- Parité mode sombre + contraste AA dans les deux thèmes.
- `:focus-visible` + navigation clavier (skip-link, ordre de tab) sur tous les contrôles.
- États vide / chargement / erreur présents et stylés (pas bruts/blancs).
- Cohérence : espacements/typo/couleurs via variables CSS (pas de valeurs one-off).
- Flux principal (init → 1er exo → check → résultat de push → rapport) sans impasse.

## Boucle
1. **Capturer + noter** : consigner chaque constat avec sa capture.
2. **Corriger UNIQUEMENT dans `piscine.css` / CSS de composants** (jetons existants). Pas de refonte,
   pas de nouvelle dépendance, **pas** de moteur/CLI/grade-received/`release.yml`, **pas** de logique
   composant. Modifs limitées à `src/Piscine.Components/**` (CSS) — jamais `Piscine.Core`/`Grading`/
   `Git`/`GitShim`/`Cli`/`Sandbox*`/`Piscine.Desktop`/`Piscine.App` (logique)/`.github`.
3. **Rebuild** : `dotnet build Piscine.slnx -c Release` (**0 warning**, TreatWarningsAsErrors) ; garder
   bUnit (`Piscine.Components.Tests`) + E2E (`Piscine.DevHost.E2E`, dont `QaProfileSmokeTests`) verts.
4. **Recapturer** la zone ; comparer avant/après. **≤ 3 itérations par zone** (anti-emballement).
5. Tout constat **subjectif** (goût, pas « objectivement cassé ») → **signalé** dans le rapport, jamais
   corrigé d'office (drapeau plutôt qu'imposition).

## Garde-fous
- Correctifs **jetons-only** ; diffs **scopés et révisables** (pas un diff géant).
- Chaque zone corrigée = commit FR conventionnel + trailer
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` ; commit ≠ push.
- Build/tests/smoke verts avant tout commit (verification before completion).

## Sortie
Rapport `docs/superpowers/audits/AAAA-MM-JJ-qa-pass.md` : galerie avant/après, constats résolus
(P1/P2/P3) avec preuve (capture), constats **signalés** (subjectifs/différés). PR(s) de refonte
scopées, chacune avec captures avant/après, auto-merge on green.

## Spot-check natif (surfaces propres à Photino, ~5 %)
Chrome de fenêtre chromeless (drag/réduire/agrandir/fermer) + terminal embarqué : via la sonde de
rendu Photino (gate local) + computer-use **quand l'accès est accordé**, sinon **checklist manuelle**
courte consignée dans le rapport. Volontairement mince (le DevHost en navigateur couvre l'UI partagée).
