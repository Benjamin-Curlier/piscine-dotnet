# Itération 10 — Module 02 « Boucles » — Implementation Plan

> **For agentic workers:** itération de **contenu**. Vérification = `piscine validate-content` vert (local + gate CI). Cases `- [ ]` pour le suivi.

**Goal:** Livrer le Module 02 `02-boucles` : itération avec `for`/`while`/`foreach`. Trois exercices `io` progressifs (compte à rebours, table de multiplication, somme de 1..N) avec `starter/` + `solution/`, un `cours.md`, validés par `validate-content`.

**Architecture / approche :** Même modèle que les Modules 00/01 (It.8/It.9) : exercices `io`, `Console.ReadLine()` + `int.Parse` pour l'entrée, `Console.WriteLine` pour la sortie (le grader normalise `\r\n`→`\n`). Pas de code applicatif modifié.

**Tech Stack:** Markdown + YAML + C#.

**Contexte repo (It.0→It.9) :** moteur complet ; modèle d'exercice `io` établi par M00/M01 (manifest `grading[].type: io` + `cases[]` `stdin`/`expect_stdout`/`expect_exit`). `validate-content` (gate CI) exige que chaque `solution/<livrable>` passe ses graders. Suite : 66 tests verts. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits FR finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. **`git commit` et `git push` en appels SÉPARÉS.**

---

## File Structure

```
content/modules/02-boucles/
├── module.yaml          # order: 2, groupe "iteration": [ex00-compte-rebours, ex01-table, ex02-somme-n]
├── cours.md             # for, while, accumulateur, foreach (mention), index
└── exercises/
    ├── ex00-compte-rebours/  {manifest.yaml, subject.md, starter/CompteRebours.cs, solution/CompteRebours.cs}
    ├── ex01-table/           {manifest.yaml, subject.md, starter/Table.cs, solution/Table.cs}
    └── ex02-somme-n/         {manifest.yaml, subject.md, starter/SommeN.cs, solution/SommeN.cs}
```

---

## Task 1 : Module + cours

- [ ] `module.yaml` : `id: 02-boucles`, `order: 2`, `course: cours.md`, groupe `iteration` ordonné `[ex00-compte-rebours, ex01-table, ex02-somme-n]`.
- [ ] `cours.md` : la boucle `while`, la boucle `for` (init/condition/incrément), l'**accumulateur** (`somme += i`), mention de `foreach` (collections, vu plus tard), ancres `#compte-rebours`, `#table`, `#somme-n`. Refs externes.

## Task 2 : `ex00-compte-rebours`

- [ ] manifest `io`, deliverable `CompteRebours.cs`, cas `"3\n"`→`"3\n2\n1\n"`, `"1\n"`→`"1\n"`, `"5\n"`→`"5\n4\n3\n2\n1\n"`.
- [ ] subject.md, starter, solution/CompteRebours.cs :
```csharp
var n = int.Parse(System.Console.ReadLine());
for (var i = n; i >= 1; i--)
{
    System.Console.WriteLine(i);
}
```

## Task 3 : `ex01-table`

- [ ] manifest `io`, deliverable `Table.cs`, cas `"2\n"`→`"1 x 2 = 2\n...10 x 2 = 20\n"`, `"5\n"`→table de 5.
- [ ] subject.md, starter, solution/Table.cs :
```csharp
var n = int.Parse(System.Console.ReadLine());
for (var i = 1; i <= 10; i++)
{
    System.Console.WriteLine($"{i} x {n} = {i * n}");
}
```

## Task 4 : `ex02-somme-n`

- [ ] manifest `io`, deliverable `SommeN.cs`, cas `"5\n"`→`"15\n"`, `"10\n"`→`"55\n"`, `"1\n"`→`"1\n"`.
- [ ] subject.md, starter, solution/SommeN.cs :
```csharp
var n = int.Parse(System.Console.ReadLine());
var somme = 0;
for (var i = 1; i <= n; i++)
{
    somme += i;
}
System.Console.WriteLine(somme);
```

## Task 5 : Vérification + push + CI

- [ ] **Local** : `$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content` → « Contenu valide. ». `... -- list` montre M00 + M01 + M02.
- [ ] **Suite** : `dotnet test Piscine.slnx --configuration Release` → 66 verts (code inchangé).
- [ ] **Commit** `content(m02): module Boucles (compte-rebours, table, somme-n, io)` puis **push séparé** + `gh run watch` → CI verte (la gate corrige les 3 corrigés M02).

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.10) :** module `02-boucles` + cours + 3 exercices `io` (boucle descendante, boucle à bornes fixes, accumulateur) validés par `validate-content`. ✓
**Risque :** parsing/newline → mêmes garanties qu'It.8/9 (`Normalize`, `int.Parse`). Prouvé par `validate-content` local.
**Reporté :** It.11+ (M03→M23) + Rushes ; grader `unit` réel à M13 ; commande `piscine new exercise` (non implémentée).
