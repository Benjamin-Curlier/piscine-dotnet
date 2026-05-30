# ex00-applique — Appliquer une fonction

## Objectif

Lis un entier **N**, puis **N** entiers (un par ligne). Stocke une **fonction** dans une variable
grâce à une **lambda** : `Func<int, int> carre = x => x * x;`. Pour chaque entier lu, affiche son
**carré** (un par ligne).

Exemple : `3` puis `2`, `3`, `4` → `4`, `9`, `16`.

## Livrable

- `Applique.cs`

## Indices

- `Func<int, int>` est dans `System` : mets `using System;` en tête.
- Une lambda est une mini-fonction sans nom : `x => x * x` prend `x` et renvoie `x * x`.
- On l'appelle comme une méthode : `carre(n)`.
- Boucle `for` de `0` à `N`, lis chaque entier avec `int.Parse(System.Console.ReadLine())`,
  puis `System.Console.WriteLine(carre(n))`.
