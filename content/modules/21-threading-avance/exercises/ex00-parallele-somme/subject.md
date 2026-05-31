# ex00-parallele-somme — Somme parallèle

## Objectif

Lis un entier **N**. Calcule la somme `1 + 2 + ... + N` **en parallèle** : répartis le travail
sur plusieurs cœurs avec `Parallel.For`, en cumulant chaque valeur dans un accumulateur
**protégé** par `Interlocked.Add` (addition atomique). Affiche le **total**.

Exemple : `100` → `5050`.

## Livrable

- `ParalleleSomme.cs`

## Indices

- Lis l'entier : `var n = int.Parse(System.Console.ReadLine());`.
- Déclare l'accumulateur en `long` : `long total = 0;`.
- `System.Threading.Tasks.Parallel.For(1, n + 1, i => ...)` exécute le corps pour `i` allant de
  `1` à `n` inclus, réparti sur plusieurs threads.
- Plusieurs threads écrivent dans `total` **en même temps** : sans protection, c'est une *race
  condition*. Utilise `System.Threading.Interlocked.Add(ref total, i);` pour une addition atomique.
- Le résultat d'une somme ne dépend pas de l'ordre : la sortie reste **déterministe**.
- `using System.Threading;` (pour `Interlocked`) et `using System.Threading.Tasks;` (pour
  `Parallel`) sont nécessaires.
