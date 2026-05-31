# ex01-compteur-static — Compteur d'instances

## Objectif

Définis une classe `Objet` avec un **champ statique** `Count` (initialement `0`).
Chaque fois qu'une instance d'`Objet` est construite, `Count` est **incrémenté de 1**.

Lis un entier **N**, crée **N** instances d'`Objet`, puis affiche la valeur de `Objet.Count`.

Exemple : entrée `3` → sortie `3`.

## Livrable

- `CompteurStatic.cs`

## Indices

- Un champ `static` est **partagé par toutes les instances** de la classe.
- Incrémente-le dans le **constructeur** : `Count++;`.
- Accède à la valeur via le **nom du type** : `Objet.Count` (pas via une instance).
- La classe `Objet` doit être déclarée **après** le code principal (top-level).
