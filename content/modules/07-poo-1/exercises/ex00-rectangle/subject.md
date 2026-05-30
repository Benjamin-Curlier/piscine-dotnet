# ex00-rectangle — Rectangle

## Objectif

Lis une **largeur** puis une **hauteur** (un entier par ligne). Crée un objet `Rectangle` et affiche
son **aire** (largeur × hauteur), calculée par une **méthode** `Aire()`.

Exemples : `3` puis `4` → `12` · `5` puis `5` → `25` · `10` puis `1` → `10`.

## Livrable

- `Rectangle.cs`

## Indices

- Déclare une classe `Rectangle` avec deux propriétés `Largeur` et `Hauteur` (`{ get; set; }`).
- Ajoute `public int Aire() => Largeur * Hauteur;`.
- Crée l'objet avec un *object initializer* : `new Rectangle { Largeur = l, Hauteur = h }`.
- Place la classe **après** les instructions principales.
