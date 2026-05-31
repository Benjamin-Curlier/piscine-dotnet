# ex01-tri-insertion — Tri par insertion

## Objectif

Lis un entier **N** (nombre d'éléments), puis **N entiers séparés par des espaces** sur la
deuxième ligne. Trie le tableau par ordre **croissant** en utilisant le **tri par insertion**
implémenté à la main. Affiche le résultat : les entiers séparés par un espace.

Exemple : `5` puis `5 3 1 4 2` → `1 2 3 4 5`

## Livrable

- `TriInsertion.cs`

## Contraintes

- **Interdit** : `Array.Sort`, `List.Sort`, LINQ `.OrderBy()` ou tout tri de la bibliothèque standard.
- Implémente l'algorithme **manuellement**.

## Indices

- Le tri par insertion « insère » chaque nouvel élément à sa place dans la partie déjà triée.
- Boucle externe `i` de 1 à n-1 : retiens `t[i]` dans une variable `cle`.
- Boucle interne : décale vers la droite tous les éléments `t[j] > cle` (en partant de `j = i - 1`
  vers 0).
- Quand la boucle interne s'arrête, place `cle` en `t[j + 1]`.
- Pense à initialiser `j` **avant** la boucle `while`, et à la décrémenter à l'intérieur.
