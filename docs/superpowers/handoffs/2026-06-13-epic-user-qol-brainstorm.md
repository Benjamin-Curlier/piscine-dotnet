# HANDOFF — Brainstorming d'un épic « QoL utilisateur »

> **But de ce document** : amorcer une **future session de brainstorming** sur un **épic
> d'amélioration de la qualité de vie (QoL) de l'utilisateur** de Piscine .NET. Il donne le contexte,
> les pistes-graines, les invariants à respecter et les normes de travail apprises — pour démarrer
> **à froid** sans re-explorer.
>
> ⚠️ **Ce n'est pas un périmètre figé.** Les idées listées en §4 sont des **graines**, pas des
> décisions. La session doit **brainstormer** (cf. §6), pas implémenter.

## 0. Démarrage (à faire en premier)

1. **`git fetch` puis comparer `main` à `origin/main`** — le checkout local peut être très en retard
   (cf. mémoire `session-start-fetch-first`). Lire l'état réel avant de raisonner.
2. Lire `docs/superpowers/HANDOFF.md` (état global, archi, format de contenu, pièges).
3. Lancer la skill **`superpowers:brainstorming`** avec ce document comme entrée. Le terminal de ce
   flux est `writing-plans` (puis décomposition en épic) — **ne pas coder avant un design validé**.

## 1. État du projet (au 2026-06-13)

- Bootcamp « piscine » C#/.NET 10. Livrable = paquet autonome (cours + exos + appli) qui sert
  d'**UX recrue** ET de **moulinette auto-correctrice** locale (sans SDK). Rendu via **vrai git**.
- **Release courante `v3.1.1`** ; **milestone v3.2 (durcissement & dette) entièrement livré**,
  **0 issue ouverte**. `main` vert.
- **UX recrue = app de bureau PhotinoX.Blazor** (`src/Piscine.Desktop` rend la RCL `src/Piscine.Components`)
  + CLI `piscine` conservé. Le moteur (`Piscine.Core`/`Grading`/`Git`/`Sandbox`) et `grade-received`
  (dans le hook git) sont **headless et stables**.
- Design produit : `docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md` ;
  desktop v4 : `docs/superpowers/specs/2026-06-06-v4-photino-desktop-design.md`.

## 2. Qui est « l'utilisateur » (à clarifier en brainstorming)

- **Recrue (primaire)** : souvent débutante. Parcourt cours/exos dans l'app, rend via git, reçoit un
  retour éducatif. C'est elle qui « vit » la piscine au quotidien.
- **Encadrant / mainteneur (secondaire)** : déploie, suit la progression, ajoute du contenu.
- **Première question de brainstorming probable** : *l'épic vise-t-il la QoL recrue, encadrant, ou les
  deux ?* (le périmètre en dépend fortement).

## 3. Surface utilisateur actuelle (où regarder)

Pages/flux recrue (RCL `Piscine.Components`, rendues par Desktop **et** le harnais `Piscine.DevHost`) :
- **`/` + `/module` + exercice** : lecteur de cours/sujets (Markdig, sommaire `CourseToc`, mode sombre).
- **`/check`** : vérification in-process d'un exo (verdict + diff attendu/obtenu + indice + lien cours).
- **`/progress`** : statut par exo (NonCommencé/EnCours/Commité-non-poussé/Poussé-noté/À revoir).
- **`/init`** : initialisation du workspace + dépôt bare + hook.
- **`/resultat`** : résultat de push **riche** (diff/indice/cours), auto-rafraîchi.
- **`/terminal`** : terminal embarqué (xterm.js + PTY) + **coaching git** (cartes de conseils, sans note ;
  sortie coalescée depuis #71).
- **CLI** `piscine` : `list/start/check/try/status/init/grade-received/validate-content/...`.

## 4. Pistes-graines de QoL (entrée de brainstorming — à challenger, prioriser, élaguer)

Tirées des limites connues, des suivis différés et de l'audit du 2026-06-13. **YAGNI** : la session en
gardera peu.

- **« Que faire ensuite ? »** : guidage clair vers le prochain exo / la prochaine étape (la progression
  actuelle décrit l'état, pas forcément l'action suivante).
- **Attribution de progression plus fine** : `PousseNote` reste *best-effort* (`progress.json` ne
  distingue pas un check local d'un grade-received). Clarté du statut.
- **Onboarding / première ouverture** : friction du setup webview, premier lancement, clarté de `/init`,
  message d'accueil, « par où commencer ».
- **Lisibilité du retour** : rendu du diff, surfaçage des indices/`course_ref`, ton éducatif cohérent,
  messages d'erreur compréhensibles pour un débutant.
- **Navigation / recherche** dans le curriculum (40 modules) ; reprise « là où je m'étais arrêté » ;
  tableau de bord de progression.
- **Coaching git** : enrichir les conseils, anticiper les erreurs fréquentes (le shim git émet déjà les
  commandes ; règles dans `CoachingService`).
- **QoL encadrant** : visibilité sur la progression d'une cohorte (aujourd'hui purement locale par recrue) —
  *attention : impliquerait potentiellement de la collecte de données → forte décision produit/vie privée.*
- **Accessibilité / raccourcis clavier** ; cohérence visuelle ; perfs perçues.
- **Distribution** : friction installeurs (AppImage Linux *online only* depuis v3.1.0).

## 5. Invariants à NE PAS casser (contraintes du domaine)

- **Modèle pédagogique** : **retour éducatif, JAMAIS de note** ; correction **par groupe, stop au 1er KO**
  (un exo `bonus` ne bloque pas la suite).
- **Déterminisme** des graders (sorties stables, indépendantes de l'ordre/culture).
- **Moteur + CLI headless inchangés** par principe : `grade-received` tourne dans le hook → reste CLI.
  La QoL porte surtout sur la **surface recrue** (app + présentation), pas sur le cœur de notation.
- **La recrue ne reçoit jamais les corrigés** dans le paquet livré (`package-content` exclut `solution/`).
- **Recrue potentiellement débutante** : ne pas présupposer une expertise git/.NET.
- **Données / vie privée** : tout suivi « cohorte » côté encadrant est une décision sensible (le projet
  est local-first, sans télémétrie).

## 6. Processus attendu pour l'épic

1. **Brainstorming** (`superpowers:brainstorming`) : clarifier l'utilisateur cible, le(s) problème(s)
   QoL prioritaires, 2-3 approches, puis un **design** validé → spec dans
   `docs/superpowers/specs/AAAA-MM-JJ-<topic>-design.md`.
2. **Un épic = trop gros pour un seul plan** → **décomposer** en sous-projets/issues. Créer un
   **milestone** (comme V3/v4/v5/v3.2) + des **issues** (label dédié).
3. **Boucle SCRUM** : 1 sprint = 1 issue → plan (`docs/superpowers/plans/`) → impl → revue → docs →
   retex (`docs/superpowers/retex/`) → **PR mergée**. Branche par sprint.

## 7. Normes de travail (apprises cette session — éviter de les ré-apprendre)

- **Brancher AVANT d'éditer** : après tout merge (surtout `gh pr merge --delete-branch`, qui supprime la
  branche locale **et** bascule sur `main`), vérifier `git branch --show-current` puis `git switch -c`.
  *(Slip récurrent cette session — corrigé à chaque fois, mais à éviter.)*
- **Agents en arrière-plan peu fiables ici** : plusieurs ont calé en silence (output 0-octet, pas de notif).
  Pour du contenu/des tâches fines, **faire inline** ; si délégation, **surveiller l'état du worktree**
  (git/FS), pas la notification, et **toujours `git -C <wt> status`** avant de pousser une branche d'agent.
- **CI** : `ci.yml` a un job `changes` (dorny/paths-filter) — les PR **docs/contenu/tests** *skippent* les
  jobs lourds (AppImage/installeur) ; les PR touchant `src/`/`build/`/csproj/workflows les déclenchent.
- **Contenu** : IDs d'exercice **globalement uniques** (ContentLocator résout par id) ; méthode =
  corrigé d'abord, `piscine try <exo> --write` pour générer les `expect_stdout` (ne pas deviner),
  `validate-content` = gate. Règles Roslyn : aucun implicit using, `System.Console.*` qualifié, déterminisme.
- **Commits** : conventionnels **en français**, terminés par
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. Commit et push en **appels séparés**.
- **Tag/release = action proprio** (irréversible — demander le go).

## 8. Pointeurs utiles

- État global : `docs/superpowers/HANDOFF.md`
- Audit de consolidation (santé, dette, pistes) : `docs/superpowers/audits/2026-06-13-consolidation-audit.md`
- Pièges Blazor/Photino/RCL/tests : mémoire `piscine-v4-blazor-photino-gotchas`
- Curriculum (état des modules + exos) : `docs/wiki/Curriculum.md`
- Mise en œuvre recrue / déploiement : `docs/mise-en-oeuvre.md`, `docs/deploiement.md`
