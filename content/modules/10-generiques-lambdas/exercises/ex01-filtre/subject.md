# ex01-filtre — Filtrer avec un prédicat

## Objectif

Lis un entier **N**, puis **N** entiers (un par ligne). Déclare un **prédicat** (une lambda qui
renvoie un booléen) : `Func<int, bool> estPair = x => x % 2 == 0;`. Affiche **seulement les
entiers pairs**, un par ligne, dans l'ordre où ils ont été lus.

Exemple : `5` puis `1`, `2`, `3`, `4`, `5` → `2`, `4`.

## Livrable

- `Filtre.cs`

## Indices

- `Func<int, bool>` est dans `System` : mets `using System;` en tête.
- Un **prédicat** est une fonction qui répond par oui/non (`bool`).
- `x % 2 == 0` est vrai quand `x` est pair.
- Boucle `for`, lis chaque entier, puis `if (estPair(n)) System.Console.WriteLine(n);`.
