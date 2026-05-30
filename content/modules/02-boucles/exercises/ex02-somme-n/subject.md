# ex02-somme-n — Somme de 1 à N

## Objectif

Lis **un entier** `N` sur l'entrée standard, puis affiche la somme `1 + 2 + … + N`.

Exemples : `5` → `15` (1+2+3+4+5) · `10` → `55` · `1` → `1`.

## Livrable

- `SommeN.cs`

## Indices

- Déclare un **accumulateur** `var somme = 0;` **avant** la boucle.
- À chaque tour de `1` à `N`, fais `somme += i;`.
- Affiche `somme` **après** la boucle.
