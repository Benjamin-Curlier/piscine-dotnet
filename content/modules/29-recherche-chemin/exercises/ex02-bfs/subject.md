# ex02-bfs — Plus court chemin (BFS)

## Objectif

Trouve le **nombre minimal de pas** pour aller du départ `S` à l'arrivée `E` sur une grille,
en se déplaçant dans les 4 directions (jamais en diagonale, jamais à travers un mur).

Entrée :
- une ligne `H W` ;
- `H` lignes de grille : `S` = départ, `E` = arrivée, `#` = mur, `.` = case libre.

Affiche le nombre minimal de pas. Si `E` est inaccessible depuis `S`, affiche `IMPOSSIBLE`.

Exemple :
```
3 3
S..
.#.
..E
```
→ `4`

## Livrable

- `Bfs.cs`

## Contraintes

- Interdit d'utiliser une bibliothèque de pathfinding : implémente le **BFS** à la main.

## Indices

- Le **parcours en largeur** (BFS) explore la grille par « anneaux » de distance croissante :
  la première fois qu'on atteint une case, c'est par le plus court chemin.
- Utilise une file `System.Collections.Generic.Queue<(int, int)>` et un tableau
  `distance[H, W]` initialisé à `-1` (= non visité).
- Enfile `S` avec distance `0`. Tant que la file n'est pas vide, dépile une case et, pour chacun
  de ses 4 voisins libres encore à `-1`, affecte `distance + 1` et enfile-le.
- À la fin, `distance[E]` vaut `-1` si E n'a jamais été atteint → `IMPOSSIBLE`.
