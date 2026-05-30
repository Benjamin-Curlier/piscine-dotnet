# Itération 9 — Module 01 « Bases C# » — Implementation Plan

> **For agentic workers:** itération de **contenu**. Vérification = `piscine validate-content` vert (local + gate CI). Cases `- [ ]` pour le suivi.

**Goal:** Livrer le Module 01 `01-bases-csharp` : types/variables, I/O console, opérateurs, conditions. Trois exercices `io` progressifs (somme, parité, maximum) avec `starter/` + `solution/`, un `cours.md`, validés par `validate-content`.

**Architecture / approche :** Même modèle que le Module 00 (It.8) : exercices `io`, `Console.ReadLine()` + `int.Parse` pour l'entrée, `Console.WriteLine` pour la sortie (le grader normalise `\r\n`→`\n`). Pas de code applicatif modifié.

**Tech Stack:** Markdown + YAML + C#.

**Contexte repo (It.0→It.8) :** moteur complet ; modèle d'exercice `io` établi par `content/modules/00-setup-git/` (manifest `grading[].type: io` + `cases[]` `stdin`/`expect_stdout`/`expect_exit`). `validate-content` (gate CI) exige que chaque `solution/<livrable>` passe ses graders. `ExerciseSubmission.IsEmpty` gère les soumissions vides (message éducatif). Suite : 66 tests verts. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits FR finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. **`git commit` et `git push` en appels SÉPARÉS.**

---

## File Structure

```
content/modules/01-bases-csharp/
├── module.yaml          # order: 1, groupe "variables-conditions": [ex00-somme, ex01-parite, ex02-maximum]
├── cours.md             # types, variables, int.Parse, opérateurs, if/else, commits atomiques
└── exercises/
    ├── ex00-somme/      {manifest.yaml, subject.md, starter/Somme.cs, solution/Somme.cs}
    ├── ex01-parite/     {manifest.yaml, subject.md, starter/Parite.cs, solution/Parite.cs}
    └── ex02-maximum/    {manifest.yaml, subject.md, starter/Maximum.cs, solution/Maximum.cs}
```

---

## Task 1 : Module + cours

- [ ] `module.yaml` : `id: 01-bases-csharp`, `order: 1`, `course: cours.md`, groupe `variables-conditions` ordonné `[ex00-somme, ex01-parite, ex02-maximum]`.
- [ ] `cours.md` : types (`int`, `double`, `string`, `bool`), `var`, lire/convertir l'entrée (`Console.ReadLine()` + `int.Parse`), opérateurs (`+ - * / %`, comparaisons), conditions (`if`/`else`, ternaire `?:`), encart **commits atomiques** (git). Ancres `#somme`, `#parite`, `#maximum`. Refs externes.

## Task 2 : `ex00-somme`

- [ ] manifest `io`, deliverable `Somme.cs`, cas `stdin: "3\n4\n"` → `"7\n"` et `stdin: "10\n-2\n"` → `"8\n"`.
- [ ] subject.md, starter/Somme.cs (squelette), solution/Somme.cs :
```csharp
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());
System.Console.WriteLine(a + b);
```

## Task 3 : `ex01-parite`

- [ ] manifest `io`, deliverable `Parite.cs`, cas `"4\n"`→`"pair\n"`, `"7\n"`→`"impair\n"`, `"0\n"`→`"pair\n"`.
- [ ] subject.md, starter, solution/Parite.cs :
```csharp
var n = int.Parse(System.Console.ReadLine());
System.Console.WriteLine(n % 2 == 0 ? "pair" : "impair");
```

## Task 4 : `ex02-maximum`

- [ ] manifest `io`, deliverable `Maximum.cs`, cas `"3\n8\n"`→`"8\n"`, `"10\n2\n"`→`"10\n"`, `"5\n5\n"`→`"5\n"`.
- [ ] subject.md, starter, solution/Maximum.cs :
```csharp
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());
System.Console.WriteLine(a > b ? a : b);
```

## Task 5 : Vérification + push + CI

- [ ] **Local** : `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content` → « Contenu valide. ». `... -- list` montre M00 + M01.
- [ ] **Suite** : `dotnet test Piscine.slnx --configuration Release` → 66 verts (code inchangé).
- [ ] **Commit** `content(m01): module Bases C# (somme, parite, maximum, io)` puis **push séparé** + `gh run watch` → CI verte (la gate corrige les 3 corrigés M01).

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.9) :** module `01-bases-csharp` + cours + 3 exercices `io` (arithmétique, condition modulo, comparaison) validés par `validate-content`. ✓
**Risque :** parsing/newline → mêmes garanties qu'It.8 (`Normalize`, `int.Parse`). Prouvé par `validate-content` local.
**Reporté :** It.10+ (M02 Boucles → M23) + Rushes ; grader `unit` réel à M13 ; commande `piscine new exercise` (non implémentée).
