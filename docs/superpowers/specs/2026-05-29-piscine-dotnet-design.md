# Piscine .NET — Design

> Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux C# (.NET 10), avec moulinette auto-correctrice locale, apprentissage du vrai git, et distribution autonome.

- **Date** : 2026-05-29
- **Statut** : Validé (design), prêt pour plan d'implémentation
- **Références d'inspiration** : [42-Piscine](https://github.com/waltergcc/42-Piscine), [CPool](https://github.com/valentin-b99/CPool)

---

## 1. Objectif & contexte

Accueillir de nouvelles recrues (bagage technique variable, souvent zéro .NET) via un bootcamp auto-rythmé inspiré de la piscine C/C++. Les recrues apprennent :

1. Les **fondamentaux de la programmation en C# pur** (algorithmie, POO, collections, async, tests), du débutant jusqu'à un palier avancé proche de la stack de production.
2. Le **vrai workflow git/GitLab** (l'équipe utilise GitLab), pratiqué à chaque rendu.

Le livrable est un **zip autonome** (cours + exercices + application console). L'application console sert à la fois d'interface recrue et de **moulinette** (auto-correcteur). Les rendus passent par un **vrai dépôt git local** ; un `git push` déclenche la moulinette.

### Décisions de cadrage (figées)

| Sujet | Décision |
|---|---|
| Objectif de sortie | Fondamentaux **C# pur** (pas de framework web dans la piscine) |
| Notation | **Hybride** : tests xUnit cachés + comparaison E/S style 42 |
| Rendu / git | **Dépôt bare local** simulant GitLab + hook déclenchant la moulinette |
| Progression | **Modulaire, séquentiel souple** (suivi local, tout reste accessible) |
| Norme / style | **Souple** : analyse Roslyn (Formatter + `.editorconfig`), non bloquante |
| Environnement | **Auto-contenu** : Roslyn embarqué (zéro SDK) + git portable bundlé |
| Langue | **Français** (cours, sujets, messages) |
| Hébergement | **GitHub** + CI/CD **.NET 10** |
| Philosophie moulinette | **Retour éducatif, jamais de note scolaire** |
| Correction | **Par groupe, séquentielle, stop au 1er échec** |
| Pression de temps | **Aucune** (pas de notion de jour/calendrier) |

---

## 2. Principes structurants

- **Retour éducatif, pas de note** : la moulinette explique *attendu vs obtenu*, donne des indices et renvoie vers la section de cours concernée. Statuts : **Réussi / À revoir / Non corrigé**. Pas de score chiffré.
- **Correction séquentielle par groupe, arrêt au premier échec** : dans un groupe d'exercices ordonné, si un exercice est KO, les suivants passent en **Non corrigé** (comme la trace 42).
- **Extensibilité data-driven** : ajouter un exercice = déposer un dossier de données, **sans recompiler** l'application.
- **Zéro pré-requis (au mieux)** : compilation C# via Roslyn embarqué ; git portable bundlé (MinGit sous Windows ; git quasi toujours présent sous Linux/macOS).
- **Auto-validation** : la CI garantit que chaque exercice livré est cohérent et que son corrigé de référence passe ses propres graders.

---

## 3. Architecture

### 3.1 Arborescence du repo (source, sur GitHub)

```
piscine-dotnet/
├── .github/workflows/
│   ├── ci.yml              # build + tests + lint + validate-content (PR & push)
│   └── release.yml         # publie les zips self-contained par OS sur la Release
├── src/
│   ├── Piscine.Cli/        # app console : UX recrue + commandes moulinette (entrée)
│   ├── Piscine.Core/       # domaine : modèle d'exercice, parsing manifest, progression
│   ├── Piscine.Grading/    # moteur Roslyn : compilation + graders (unit, io, norme)
│   └── Piscine.Git/        # LibGit2Sharp : dépôt bare, hooks, lecture des commits
├── tests/                  # xUnit pour chaque projet src (le bootcamp se teste lui-même)
│   ├── Piscine.Core.Tests/
│   ├── Piscine.Grading.Tests/
│   └── Piscine.Git.Tests/
├── content/                # 100% des contenus pédagogiques (data-driven)
│   ├── modules/
│   │   └── 00-setup-git/
│   │       ├── module.yaml
│   │       ├── cours.md
│   │       └── exercises/
│   │           └── ex00-hello/
│   │               ├── manifest.yaml
│   │               ├── subject.md
│   │               ├── starter/        # fichiers fournis à la recrue
│   │               ├── grader/         # tests cachés / sortie attendue
│   │               └── solution/       # corrigé de référence (CI uniquement, jamais zippé)
│   └── rushes/
├── docs/                   # guide contributeur « ajouter un exercice » + specs
├── build/                  # script d'assemblage du zip distribuable
└── Piscine.sln
```

### 3.2 Découpage des projets (responsabilités, interfaces)

- **Piscine.Core** — modèle de domaine pur. Parse `module.yaml`/`manifest.yaml`, découvre le contenu en scannant `content/`, gère le store de progression. Aucune dépendance à Roslyn ou git. *Testable isolément.*
- **Piscine.Grading** — moteur de correction. Compile le code recrue via Roslyn, exécute les graders en contexte isolé (`AssemblyLoadContext`), produit des résultats éducatifs structurés. Dépend de Core.
- **Piscine.Git** — wrapper LibGit2Sharp : init workspace, dépôt bare « remote », installation du hook `post-receive`, checkout d'un commit reçu. Dépend de Core.
- **Piscine.Cli** — point d'entrée. Menu d'accueil, commandes (`init`, `start`, `check`, `grade-received`, `validate-content`, `new exercise`), rendu du feedback et de la progression. Orchestre les trois autres.

### 3.3 Artefact distribué (le zip que reçoit la recrue, produit par la CI)

- **Binaire CLI self-contained single-file** par OS : `win-x64`, `linux-x64`, `osx-arm64` (runtime .NET + Roslyn inclus, zéro install).
- **`content/`** : cours + sujets + starters + graders. **Les `solution/` sont exclus.**
- **git portable** (MinGit sous Windows) + script de lancement.
- La recrue dézippe et lance `piscine` → menu d'accueil.

**Anti-triche** : tout étant local, les `grader/` sont présents sur la machine. Vu l'objectif « apprentissage, pas note », ils sont **empaquetés en ressource compactée** pour décourager la lecture, sans prétendre à une sécurité serveur.

---

## 4. Format d'un exercice (extensibilité)

### 4.1 `module.yaml`

```yaml
id: 00-setup-git
title: "Mise en place & premiers pas Git"
order: 0
course: cours.md
groups:
  - id: premiers-commits
    title: "Premiers commits"
    exercises: [ex00-hello, ex01-identite]   # ordonné → stop au 1er KO
  - id: branches-fusion
    title: "Branches & fusion"
    exercises: [ex02-branche]
```

> Les `groups` sont **thématiques**, jamais calendaires (aucune pression de temps).

### 4.2 `manifest.yaml` (un par exercice, 100% déclaratif)

```yaml
id: ex00-hello
title: "Hello, Piscine"
objective: "Écrire un programme qui affiche un message précis."
deliverables: [Hello.cs]            # ce que la recrue doit rendre
starter:      [starter/README.md]   # fichiers fournis au départ
constraints:                        # garde-fous pédagogiques (optionnel)
  forbidden_apis: ["System.IO.File"]
  forbidden_keywords: []
grading:                            # types combinables (hybride)
  - type: io                        # exécute le programme, compare stdout/exit
    cases:
      - args: []
        stdin: ""
        expect_stdout: "Hello, Piscine!\n"
        expect_exit: 0
  - type: unit                      # compile code recrue + tests xUnit cachés
    test_files: [grader/HelloTests.cs]
  - type: norme                     # analyse Roslyn (Formatter + .editorconfig)
    blocking: false                 # souple : avertissement, ne bloque pas
feedback:
  hints:
    - when: io_mismatch
      message: "Vérifie la casse, le '!' et le retour à la ligne final."
  course_ref: "cours.md#hello-world"
solution: [solution/Hello.cs]       # corrigé de référence — CI uniquement, jamais zippé
```

### 4.3 Les trois types de grader (moteur Roslyn, in-process, isolé)

- **`io`** : compile en exécutable, lance dans un `AssemblyLoadContext` isolé, injecte args/stdin, compare stdout/stderr/exit → diff éducatif.
- **`unit`** : compile code recrue + tests cachés + référence xUnit, exécute les tests, récupère les messages d'assertion → feedback.
- **`norme`** : diagnostics Roslyn (Formatter + `.editorconfig`), non bloquant.

**Porte de sortie (future, hors v1)** : un `type: custom` pointant vers un grader C# compilé, pour les cas qui ne rentrent pas dans le déclaratif.

### 4.4 Ajouter un exercice (workflow contributeur)

1. `piscine new exercise <module> <id>` → génère le squelette (manifest + subject + starter + grader + solution).
2. Remplir l'énoncé, les fichiers attendus et les graders.
3. Ajouter l'`id` dans un groupe de `module.yaml`.
4. **Aucune recompilation** : la CLI scanne `content/` au démarrage.

### 4.5 Garde-fou qualité

**`piscine validate-content`** (lancée aussi en CI) vérifie pour chaque exercice : manifest valide, fichiers référencés présents, graders qui compilent, et que le **corrigé `solution/` passe bien ses propres graders**. → impossible de merger un exercice cassé.

---

## 5. Flux moulinette + git

### 5.1 Mise en place — `piscine init` (1er lancement)

- Crée le **workspace** recrue : `~/piscine/workspace/` (espace de code).
- Crée un **dépôt bare** `~/.piscine/remote.git` = le « GitLab » local, ajouté comme `origin`.
- Installe un hook **`post-receive`** dans le bare repo (déclencheur moulinette).
- Configure le git bundlé → la recrue tape de **vraies** commandes git.

### 5.2 Deux boucles distinctes

| Commande | Rôle | Effet |
|---|---|---|
| `piscine check [exo]` | Itération rapide, sans commit | Feedback éducatif instantané, **ne compte pas** comme rendu |
| `git push origin main` | **Rendu officiel** (vrai geste GitLab) | Le hook lance la moulinette sur le commit reçu, affiche le feedback **et enregistre la progression** |

### 5.3 Déroulé d'un `git push`

1. Le hook `post-receive` du bare repo appelle `piscine grade-received <sha>`.
2. La moulinette **checkout le commit reçu** dans un dossier temporaire isolé.
3. Elle détecte les exercices présents, les **corrige par groupe, dans l'ordre, stop au 1er KO** (suivants → *Non corrigé*).
4. Feedback éducatif imprimé pendant le push + persisté.

### 5.4 Cycle de travail type

```
piscine start ex00-hello   # copie le starter dans workspace/00-setup-git/ex00-hello/
# ... la recrue code ...
piscine check              # feedback instantané, autant de fois qu'elle veut
git add . && git commit -m "ex00" && git push origin main   # rendu officiel
```

### 5.5 Suivi de progression

`~/.piscine/progress.json` : statut par exo (*Réussi / À revoir / Non corrigé*), tentatives, dernier feedback. Le **menu d'accueil** affiche la progression et **suggère le prochain exercice** (séquentiel souple — tout reste accessible).

---

## 6. Curriculum (backlog de contenu)

Git est tissé dans tout le parcours (pratiqué à chaque rendu) + deux modules git dédiés. Le contenu est généré **progressivement** au fil des itérations.

### Tronc commun

| # | Module | Notions clés | Git |
|---|---|---|---|
| 00 | Setup & Git | installer, lancer `piscine`, hello world | clone/add/commit/push, 1er rendu |
| 01 | Bases C# | types, variables, I/O console, opérateurs, conditions | commits atomiques |
| 02 | Boucles | `for`/`while`/`foreach`, itération | messages de commit clairs |
| 03 | Méthodes | paramètres, portée, retour, récursion | — |
| 04 | Tableaux & chaînes | `array`, manipulation de `string` | `.gitignore` |
| 05 | ★ Git intermédiaire | (dédié) | branches, merge, conflits, historique |
| 06 | Collections | `List`, `Dictionary`, intro LINQ | — |
| 07 | POO 1 | classes, objets, encapsulation, propriétés | — |
| 08 | POO 2 | héritage, interfaces, polymorphisme, abstrait | — |
| 09 | Exceptions | `try/catch`, gestion d'erreurs, `Result` | — |
| 10 | Génériques & lambdas | `T`, délégués, `Func`/`Action` | — |
| 11 | LINQ | requêtes, projection, agrégation | — |
| 12 | Async/await | `Task`, asynchrone, annulation | — |
| 13 | Tests unitaires | xUnit, écrire ses propres tests | — |
| 14 | ★ Git avancé / collab | (dédié) | rebase, workflow MR GitLab, revue de code |

### Palier avancé

| # | Module | Notions clés |
|---|---|---|
| 15 | Regex | motifs, groupes, validation, `Regex` performant |
| 16 | Sérialisation | `System.Text.Json`, (dé)sérialisation, converters |
| 17 | Réflexion & attributs | `Type`, introspection, attributs custom |
| 18 | Injection de dépendances | `Microsoft.Extensions.DependencyInjection`, durées de vie |
| 19 | Logging | `Microsoft.Extensions.Logging`, niveaux, scopes, providers |
| 20 | Generic Host & Worker | `HostBuilder`, `BackgroundService`, config & options |
| 21 | Threading avancé | `Channel<T>`, producteur/consommateur, `Parallel`, synchro |
| 22 | Réseau | sockets TCP/UDP, `HttpClient` |
| 23 | Design patterns | GoF essentiels en C# (Strategy, Factory, Observer, Decorator…) |

### Rushes (solo, projets de synthèse)

- **Rush 0** (après ~M04) : programme console ludique (ASCII-art / mini-calculatrice / FizzBuzz avancé).
- **Rush 1** (après POO, ~M08) : appli métier console (gestionnaire d'inventaire/bibliothèque).
- **Rush 2** (après LINQ/async, ~M12) : CLI de traitement de données (parser un fichier, agréger, rapport).
- **Rush 3** (après palier avancé) : Worker Service complet — consomme un `Channel<T>`, I/O réseau, `ILogger`, câblé en DI sous `HostBuilder`.

### Cours générés

Chaque `cours.md` : explications progressives en français + exemples + **références externes** (Microsoft Learn, freeCodeCamp, chaînes YouTube type Nick Chapsas / Tim Corey, docs officielles .NET). Ton pédagogique, jargon expliqué.

---

## 7. CI/CD (.NET 10, GitHub Actions)

### `ci.yml` (PR & push)

1. `dotnet restore` + `build` (warnings as errors).
2. `dotnet test` (xUnit, tous les projets `src`).
3. **`piscine validate-content`** — manifests valides + corrigés `solution/` passants.
4. Vérification de format Roslyn sur le code du bootcamp lui-même.

### `release.yml` (sur tag)

- `dotnet publish` self-contained single-file × `win-x64` / `linux-x64` / `osx-arm64`.
- Assemble le zip distribuable : binaire + `content/` (**sans** `solution/`) + MinGit (Windows) + script de lancement.
- Attache les 3 zips à la GitHub Release.

---

## 8. Plan d'itérations (séquentiel)

Les tâches détaillées de chaque itération sont définies au début de l'itération, dans un plan d'implémentation dédié sous `docs/superpowers/plans/`.

> **Note de numérotation :** l'It. 2 (Moulinette) a été scindée en cours de route en deux itérations (compile+io+norme, puis unit+orchestration), ce qui a décalé la suite. La liste ci-dessous reflète l'état réel.

### Avancement (au 2026-05-29)

- **It. 0 — Bootstrap** ✅ — solution, 4 projets, `.editorconfig`, `ci.yml`, README + guide contributeur.
- **It. 1 — Core** ✅ — parsing `module.yaml`/`manifest.yaml`, `ContentDiscovery`, `ProgressStore`.
- **It. 2 — Moulinette (partie 1)** ✅ — compilation Roslyn + graders `io` et `norme` (exécution in-process isolée).
- **It. 3 — Grader unit + orchestration** ✅ — grader `unit` (xUnit réflexif), `GroupGrader` (séquentiel, stop au 1er KO), `ProgressRecorder`.
- **It. 4 — CLI UX** ✅ — commandes `list`/`start`/`check`/`status`, assemblage disque, feedback éducatif, persistance progression.

### Itérations restantes (séquentielles)

- **It. 5 — Git** : `Piscine.Git` (LibGit2Sharp) — `init` (workspace + bare remote `origin` + hook `post-receive`) + `grade-received <sha>` (checkout → `GroupGrader` → feedback + progression), MinGit bundlé (Windows). *Vérif : `git push`→moulinette de bout en bout.*
- **It. 6 — Packaging & mise en œuvre** : `release.yml`, zips self-contained par OS, `validate-content` en gate, **doc de mise en œuvre** (voir §10). Gérer le cas single-file pour `TRUSTED_PLATFORM_ASSEMBLIES`. *Vérif : 1er zip téléchargeable + installable d'après la doc.*
- **It. 7 — Wiki GitHub** : documentation projet dans le wiki du dépôt (voir §10).
- **It. 8 — Module 00 (Setup & Git) complet** : 1er vrai module, dogfood du système entier avec le parcours recrue réel.
- **It. 9+ — Contenu** : 1 (ou 2) module(s) par itération dans l'ordre du curriculum (M01→M14 puis M15→M23), + les Rushes à leurs checkpoints. Chaque itération = `cours.md` + exercices + graders + corrigés, validés par la CI.

### Point d'attention GitHub

Repo créé sur le GitHub du propriétaire (`Benjamin-Curlier/piscine-dotnet`, privé) ; `gh` est authentifié (scopes `repo`+`workflow`). L'accès reste à demander au propriétaire plutôt qu'à contourner si besoin.

---

## 9. Hors périmètre (YAGNI v1)

- Tableau de bord serveur / centralisation des résultats (tout est local).
- Sécurité anti-triche forte (empaquetage léger seulement).
- Frameworks web (ASP.NET, Blazor) — hors objectif « C# pur ».
- Graders `type: custom` compilés (prévu mais post-v1).
- Multi-langue (français uniquement en v1).

---

## 10. Documentation (livrables dédiés)

Deux documentations distinctes, en plus du `README.md` et du guide contributeur (`docs/contributing/`).

### 10.1 Doc de mise en œuvre — préparation du PC de la personne onboardée (It. 6)

Guide pas-à-pas, versionné dans le dépôt (`docs/mise-en-oeuvre.md`), destiné à **préparer la machine de la recrue** et à démarrer la piscine. À écrire à l'It. 6 (le flux d'installation dépend du zip produit). Contenu attendu :

- **Prérequis réels** : aucun SDK .NET à installer (binaire self-contained) ; git portable (MinGit) fourni dans le zip Windows ; sous Linux/macOS, git généralement déjà présent (sinon une ligne d'installation).
- **Installation** : télécharger le zip de la dernière release correspondant à l'OS, dézipper dans un dossier, (Windows : éventuelle exception SmartScreen / déblocage du binaire), lancer `piscine` / `piscine.exe`.
- **Premier lancement** : `piscine init` (workspace + dépôt bare local + hook), vérification que `git` bundlé répond, puis `piscine start <premier-exo>`.
- **Boucle de travail** : `piscine check` (itération rapide) puis rendu officiel par `git add/commit/push`.
- **Dépannage** : antivirus/permissions, chemins, variables d'environnement (`PISCINE_HOME`, `PISCINE_CONTENT`), réinitialisation.
- **Côté encadrant** : check-list de préparation poste + remise du zip à la recrue.

### 10.2 Wiki GitHub (It. 7)

Documentation projet hébergée dans le **wiki GitHub** du dépôt (dépôt git séparé `Benjamin-Curlier/piscine-dotnet.wiki.git`, édité en local puis poussé). Public visé : encadrants et contributeurs. Pages attendues :

- **Accueil / vue d'ensemble** : objectif de la piscine, philosophie (retour éducatif, pas de note), liens.
- **Fonctionnement de la moulinette** : compilation Roslyn, graders `io`/`unit`/`norme`, correction séquentielle par groupe (stop au 1er KO), progression.
- **Workflow de rendu** : dépôt bare local, hook `post-receive`, `check` vs `git push`.
- **Ajouter un exercice / un module** : reprise et approfondissement de `docs/contributing/`, format `manifest.yaml`/`module.yaml`, `validate-content`.
- **Curriculum** : carte des modules et Rushes, références externes.
- **Mise en œuvre** : lien vers la doc §10.1.
