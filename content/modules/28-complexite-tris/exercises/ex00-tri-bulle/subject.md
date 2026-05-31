# ex00-tri-bulle — Tri à bulles

## Objectif

Lis un entier **N** (nombre d'éléments), puis **N entiers séparés par des espaces** sur la
deuxième ligne. Trie le tableau par ordre **croissant** en utilisant le **tri à bulles**
implémenté à la main. Affiche le résultat : les entiers séparés par un espace.

Exemple : `5` puis `5 3 1 4 2` → `1 2 3 4 5`

## Livrable

- `TriBulle.cs`

## Contraintes

- **Interdit** : `Array.Sort`, `List.Sort`, LINQ `.OrderBy()` ou tout tri de la bibliothèque standard.
- Implémente l'algorithme **manuellement** avec des boucles et des échanges.

## Indices

- Le tri à bulles fait des **passes** sur le tableau ; à chaque passe, les plus grands éléments
  « remontent » vers la fin.
- Deux boucles imbriquées : la boucle externe `i` (0 → n-2) compte les passes ; la boucle
  interne `j` (0 → n-2-i) compare les voisins.
- Si `t[j] > t[j+1]`, échange les deux valeurs via une variable temporaire `tmp`.
- Pour lire les entiers : `System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries)` puis `int.Parse` sur chaque élément.
- Pour afficher : `string.Join(" ", t)` ne nécessite pas de `using`.
