# ex01-parse-robuste — Parse robuste

## Objectif

Lis un entier **N**, puis **N** lignes. Pour chacune, essaie de la convertir en entier avec
`int.Parse`. Si la conversion réussit, affiche le nombre. Si la ligne n'est pas un entier valide,
`int.Parse` lève une `FormatException` : attrape-la et affiche `invalide` à la place.

Exemple : `3` puis `42`, `abc`, `7` → `42`, `invalide`, `7`.

## Livrable

- `Parse.cs`

## Indices

- Ajoute `using System;` en haut (`FormatException` est dans `System`).
- Boucle `for` de `0` à `N`. À chaque tour, lis la ligne puis place `int.Parse(ligne)` dans un `try`.
- En cas d'échec, `catch (FormatException)` te permet d'afficher `invalide` sans planter le programme.
