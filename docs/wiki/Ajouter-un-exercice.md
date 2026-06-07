# Ajouter un exercice / un module

Le contenu est **data-driven** : ajouter un exercice = déposer des fichiers, **sans recompiler**
l'application (le contenu est découvert au démarrage). La référence courte est dans le dépôt :
[docs/contributing/ajouter-un-exercice.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/contributing/ajouter-un-exercice.md).

## Arborescence

```
content/modules/<NN-slug>/
├── module.yaml
├── cours.md
└── exercises/<id>/
    ├── manifest.yaml
    ├── subject.md
    ├── starter/      # fichiers fournis à la recrue
    ├── grader/       # tests cachés / données attendues
    └── solution/     # corrigé de référence (CI uniquement, JAMAIS zippé)
```

## `module.yaml`

```yaml
id: 00-setup-git
title: "Mise en place & premiers pas Git"
order: 0
course: cours.md
groups:
  - id: premiers-commits
    title: "Premiers commits"
    exercises: [ex00-hello, ex01-identite]   # ORDONNÉ → correction séquentielle, stop au 1er KO
```

> Les `groups` sont **thématiques**, jamais calendaires.

## `manifest.yaml` (un par exercice, 100 % déclaratif)

```yaml
id: ex00-hello
title: "Hello, Piscine"
objective: "Écrire un programme qui affiche un message précis."
deliverables: [Hello.cs]            # ce que la recrue doit rendre
starter:      [starter/README.md]   # fichiers fournis au départ
grading:                            # types combinables (hybride)
  - type: io                        # exécute le programme, compare stdout/exit
    cases:
      - args: []
        stdin: ""
        expect_stdout: "Hello, Piscine!\n"
        expect_exit: 0
  - type: unit                      # compile code recrue + tests xUnit cachés
    test_files: [grader/HelloTests.cs]
  - type: norme                     # analyse Roslyn (souple, non bloquant)
    blocking: false
feedback:
  hints:
    - when: io_mismatch
      message: "Vérifie la casse, le '!' et le retour à la ligne final."
  course_ref: "cours.md#hello-world"
solution: [solution/Hello.cs]       # corrigé de référence — CI uniquement
```

Voir [Moulinette](Moulinette) pour le détail des graders — outre `io` / `unit` / `norme`, le moteur
fournit aussi `mutation` (l'élève écrit ses tests), `git` (état du dépôt), `projet` (multi-fichiers +
archi) et `reseau` (écho TCP).

## Étapes

1. **Générer le squelette** : `piscine new exercise <module> <id>` (ex. `piscine new exercise 02-boucles ex03-puissance`).
   La commande crée le dossier de l'exercice avec un `manifest.yaml` `io` pré-rempli (TODO à compléter),
   un `subject.md`, et `starter/` + `solution/` contenant le livrable déduit de l'id
   (`ex03-puissance` → `Puissance.cs`). Le module ciblé doit déjà exister.
2. Renseigner `manifest.yaml` et `subject.md` (l'énoncé).
3. Placer les fichiers fournis dans `starter/`, les tests cachés dans `grader/`, le **corrigé** dans
   `solution/` (convention : `solution/<livrable>` pour chaque `deliverable`).
4. Ajouter l'`id` de l'exercice dans un **groupe** de `module.yaml` (l'ordre = correction séquentielle).
5. **Valider** : `piscine validate-content`.

## Garde-fou : `validate-content`

`piscine validate-content` vérifie, pour chaque exercice référencé : manifest valide, fichiers de
graders présents, dossier `solution/` présent, livrables présents dans le corrigé, et que le
**corrigé passe bien ses propres graders**. **La CI exécute la même commande** → un exercice cassé
ne peut pas être mergé.

> Les dossiers `solution/` sont **exclus du zip distribué** (commande `package-content`) : la recrue
> ne reçoit jamais les corrigés.
