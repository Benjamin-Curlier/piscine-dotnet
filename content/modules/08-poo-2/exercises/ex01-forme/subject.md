# ex01-forme — Aire des formes

## Objectif

Lis trois entiers, un par ligne : le **côté** d'un carré, puis la **largeur** et la **hauteur**
d'un rectangle. Crée les deux formes et affiche l'**aire** de chacune (le carré d'abord,
le rectangle ensuite), une par ligne.

Exemple : `4`, `3`, `5` → carré d'aire `16`, rectangle d'aire `15`.

## Livrable

- `Forme.cs`

## Indices

- Déclare une interface `IForme` avec une seule méthode : `int Aire();`.
- `Carre` (propriété `Cote`) et `Rectangle` (propriétés `Largeur`, `Hauteur`) implémentent `IForme`
  (`class Carre : IForme`) et fournissent leur propre `Aire()`.
- Range les deux formes dans une `List<IForme>` et appelle `Aire()` sur chacune.
- `List<>` nécessite `using System.Collections.Generic;`.
