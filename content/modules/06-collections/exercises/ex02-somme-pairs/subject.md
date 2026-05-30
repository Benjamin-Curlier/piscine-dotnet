# ex02-somme-pairs — Somme des pairs (LINQ)

## Objectif

Lis **une ligne** d'entiers séparés par des espaces, puis affiche la **somme des nombres pairs**
uniquement. S'il n'y en a aucun, affiche `0`.

Exemples : `1 2 3 4` → `6` (2+4) · `1 3 5` → `0` · `10 -4 7` → `6` (10 + (-4)).

## Livrable

- `SommePairs.cs`

## Indices

- Ajoute `using System.Linq;` tout en haut du fichier.
- Convertis les morceaux en nombres, puis enchaîne `.Where(x => x % 2 == 0).Sum()`.
- Un nombre est pair si `x % 2 == 0` (vrai aussi pour les négatifs pairs).
