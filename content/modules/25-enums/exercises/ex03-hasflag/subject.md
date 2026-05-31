# ex03-hasflag — Tester un drapeau (bonus)

> Exercice **bonus** (difficulté : difficile).

## Objectif

Lis un **entier** (une valeur de permissions déjà combinée), puis un **nom de permission**
(`Lecture`, `Ecriture` ou `Execution`). Affiche `oui` si ce drapeau est **présent** dans la
valeur combinée, sinon `non`.

L'enum est marqué `[Flags]` avec `Lecture = 1`, `Ecriture = 2`, `Execution = 4`. La valeur `3`
contient `Lecture` (1) et `Ecriture` (2) ; elle ne contient pas `Execution` (4).

Exemple : `3` puis `Lecture` → `oui` ; `4` puis `Ecriture` → `non`.

## Livrable

- `HasFlag.cs`

## Indices

- Déclare `[Flags] enum Permissions { Lecture = 1, Ecriture = 2, Execution = 4 }`.
- Reconstitue la valeur combinée par un cast : `var combinee = (Permissions)int.Parse(ligne);`.
- Convertis le nom du drapeau testé : `var p = Enum.Parse<Permissions>(nom);`.
- Teste sa présence avec `combinee.HasFlag(p)` (équivalent à `(combinee & p) == p`).
- `Enum.Parse` exige `using System;`.
