# ex01-mention — La mention

## Objectif

Lis une note (un entier) et affiche la **mention** correspondante :

- `90` et plus → `excellent`
- `50` à `89` → `passable`
- en dessous de `50` → `insuffisant`

Exemple : `95` → `excellent` ; `60` → `passable` ; `30` → `insuffisant`.

## Livrable

- `Mention.cs`

## Indices

- Un **pattern relationnel** compare la valeur : `>= 90`, `>= 50`.
- Dans une switch expression, **la première branche qui correspond gagne** : place donc le
  seuil le plus haut en premier.
- `n switch { >= 90 => "excellent", >= 50 => "passable", _ => "insuffisant" }`.
