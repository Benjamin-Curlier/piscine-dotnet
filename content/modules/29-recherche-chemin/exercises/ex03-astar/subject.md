# ex03-astar — A* sur grille pondérée

## Objectif

Trouve le **coût total minimal** d'un chemin du départ à l'arrivée sur une grille où chaque case
a un **coût d'entrée**. Implémente l'algorithme **A\*** avec l'heuristique de Manhattan.

Entrée :
- une ligne `H W` ;
- `H` lignes de grille : un **chiffre `1`–`9`** = coût pour entrer dans la case, `#` = mur ;
- une ligne `rS cS` : la case de départ ;
- une ligne `rE cE` : la case d'arrivée.

Le départ ne coûte rien ; chaque déplacement vers une case voisine ajoute le **chiffre de cette
case** au coût total. Affiche le coût minimal, ou `IMPOSSIBLE` si l'arrivée est inaccessible.

Exemple :
```
3 3
111
191
111
0 0
2 2
```
→ `4` (on contourne le `9` central : quatre cases à coût `1`).

## Livrable

- `AStar.cs`

## Contraintes

- Implémente **A\*** : Dijkstra guidé par une heuristique. Pas de bibliothèque toute faite.

## Indices

- A\* choisit toujours la case ouverte de plus petite **priorité `f = g + h`** :
  - `g` = coût réel accumulé depuis le départ ;
  - `h` = estimation du coût restant = **distance de Manhattan** jusqu'à l'arrivée (ex00).
- Utilise `System.Collections.Generic.PriorityQueue<(int, int), int>` : `Enqueue(case, f)`,
  `Dequeue()` rend la case de plus petit `f`.
- Garde un tableau `cout[H, W]` initialisé à `int.MaxValue`. Quand un nouveau chemin vers un
  voisin est moins cher, mets à jour `cout` et ré-enfile le voisin avec sa nouvelle priorité.
- L'heuristique de Manhattan ne surestime jamais le vrai coût (chaque pas coûte au moins `1`),
  donc A\* renvoie bien le coût **optimal**.
