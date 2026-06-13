# ex03-pile — Pile (LIFO) (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Conçois ta propre classe **pile** (structure **LIFO** : *Last In, First Out*) avec au moins trois
méthodes : `Empiler` (ajouter), `Depiler` (retirer le dernier ajouté) et `EstVide`.

Lis un entier `N`, puis `N` entiers (un par ligne) que tu **empiles** dans l'ordre de lecture.
Ensuite, **dépile** tout et affiche chaque valeur retirée (une par ligne). L'ordre de sortie est
donc l'**inverse** de l'ordre d'entrée.

Exemple : pour `3` puis `1`, `2`, `3`, le programme affiche `3`, `2`, `1`.

## Livrable

- `Pile.cs`

## Indices

- Encapsule une `List<int>` dans ta classe (nécessite `using System.Collections.Generic;`).
- `Empiler` ajoute en fin (`.Add`). `Depiler` lit puis retire le **dernier** élément
  (`_elements[_elements.Count - 1]` puis `.RemoveAt(...)`).
- `EstVide` renvoie `_elements.Count == 0`.
- Dépile tant que la pile n'est pas vide.
