# Module 29 — Recherche de chemin (BFS, Dijkstra, A*)

Trouver le plus court chemin d'un point A à un point B est un problème omniprésent : GPS, jeux
vidéo, robotique, réseaux. On le modélise presque toujours par un **graphe**, et sur une grille
le graphe est implicite : chaque case est un sommet, chaque déplacement autorisé une arête. Ce
module construit, brique par brique, l'algorithme **A\*** — la référence du pathfinding.

---

## 1. Grilles, coordonnées et voisinage {#voisinage}

Une grille ASCII se lit comme un tableau de chaînes. On repère une case par `(ligne, colonne)`,
**indexées à partir de 0**. La case en haut à gauche est `(0, 0)`.

```
ligne 0 →  S..
ligne 1 →  .#.
ligne 2 →  ..E
           ↑ colonne 0
```

Les **4 voisins** d'une case `(r, c)` (déplacements orthogonaux, sans diagonale) sont :

| Direction | Voisin |
|---|---|
| Haut   | `(r-1, c)` |
| Bas    | `(r+1, c)` |
| Gauche | `(r, c-1)` |
| Droite | `(r, c+1)` |

On code ces décalages dans deux tableaux parallèles, ce qui évite de répéter quatre fois la même
logique :

```csharp
var dr = new[] { -1, 1, 0, 0 };
var dc = new[] { 0, 0, -1, 1 };
for (var d = 0; d < 4; d++)
{
    var nr = r + dr[d];
    var nc = c + dc[d];
    // Toujours vérifier les bornes AVANT de lire grille[nr][nc].
    if (nr >= 0 && nr < h && nc >= 0 && nc < w && grille[nr][nc] != '#')
    {
        // voisin accessible
    }
}
```

⚠️ L'ordre dans lequel on explore les voisins n'a pas d'importance pour la *longueur* du plus
court chemin, mais il en a une pour le **chemin lui-même** quand plusieurs sont possibles. On
fixe donc un ordre (Haut, Bas, Gauche, Droite) pour rendre le résultat déterministe (ex04).

---

## 2. L'heuristique de Manhattan {#heuristique}

Une **heuristique** est une estimation rapide du coût restant pour atteindre l'arrivée. Sur une
grille à déplacements orthogonaux, l'estimation naturelle est la **distance de Manhattan** :

```
h(r, c) = |r - rArrivée| + |c - cArrivée|
```

C'est le nombre de pas qu'il faudrait *s'il n'y avait aucun mur*. Comme elle ne **surestime
jamais** le vrai coût, on dit qu'elle est **admissible** — c'est la condition qui garantit que
A\* trouve le chemin optimal (section 5).

---

## 3. BFS — plus court chemin non pondéré {#bfs}

Quand **tous les déplacements coûtent pareil** (1 pas), le **parcours en largeur** (BFS, *breadth-first
search*) suffit. Il explore la grille par anneaux de distance croissante : 0, puis 1, puis 2…
La **première fois** qu'on atteint une case, c'est forcément par un plus court chemin.

```csharp
using System.Collections.Generic;

var distance = new int[h, w];
// init à -1 = non visité (boucle omise)

var file = new Queue<(int r, int c)>();
file.Enqueue((sr, sc));
distance[sr, sc] = 0;

while (file.Count > 0)
{
    var (r, c) = file.Dequeue();
    for (var d = 0; d < 4; d++)
    {
        var nr = r + dr[d];
        var nc = c + dc[d];
        if (/* dans les bornes, pas un mur */ distance[nr, nc] == -1)
        {
            distance[nr, nc] = distance[r, c] + 1;
            file.Enqueue((nr, nc));
        }
    }
}
```

La clé : une case n'est mise en file **qu'une seule fois** (dès qu'elle quitte `-1`). Si
`distance[arrivée]` vaut encore `-1` à la fin, l'arrivée est **inaccessible**.

Complexité : **O(H × W)** — chaque case est traitée une fois.

### Reconstruire le chemin {#reconstruction}

Le BFS donne la *longueur*, mais on veut parfois le *trajet*. Il suffit de mémoriser, pour chaque
case, **par où** on y est arrivé : son prédécesseur et la lettre du déplacement. Une fois
l'arrivée atteinte, on remonte les prédécesseurs jusqu'au départ, puis on **inverse** la liste
des lettres. (C'est l'objet de l'ex04.)

---

## 4. Dijkstra — quand les cases ont un coût

Si entrer dans une case coûte plus ou moins cher (terrain : herbe = 1, boue = 5…), le BFS ne
suffit plus : le chemin le plus *court en nombre de pas* n'est plus forcément le moins *cher*.
**Dijkstra** généralise le BFS en remplaçant la file simple par une **file de priorité** qui rend
toujours la case de **plus petit coût accumulé** `g`. On relâche les voisins : si l'on trouve un
chemin moins cher vers un voisin, on met à jour son coût et on le ré-enfile.

---

## 5. A* — Dijkstra guidé par une heuristique {#astar}

**A\*** est Dijkstra qui, au lieu de classer les cases par leur seul coût réel `g`, les classe par

```
f = g + h
```

où `h` est l'heuristique (distance de Manhattan jusqu'à l'arrivée). Intuitivement, A\* préfère
explorer les cases qui *semblent* rapprocher du but, au lieu de s'étaler dans toutes les
directions. Résultat : il visite beaucoup moins de cases que Dijkstra, **pour le même chemin
optimal** — à condition que `h` soit admissible.

```csharp
using System.Collections.Generic;

var ouverts = new PriorityQueue<(int r, int c), int>();
cout[sr, sc] = 0;
ouverts.Enqueue((sr, sc), Heuristique(sr, sc));   // f = 0 + h

while (ouverts.Count > 0)
{
    var (r, c) = ouverts.Dequeue();               // plus petit f
    if (r == er && c == ec) break;
    foreach (var voisin /* libre */)
    {
        var nouveau = cout[r, c] + coutEntree(voisin);   // nouveau g
        if (nouveau < cout[voisin])
        {
            cout[voisin] = nouveau;
            ouverts.Enqueue(voisin, nouveau + Heuristique(voisin));  // f = g + h
        }
    }
}
```

- Si `h` renvoie toujours `0`, A\* **redevient Dijkstra**.
- Si en plus tous les coûts valent `1`, Dijkstra **redevient BFS**.

Ces trois algorithmes sont donc une même idée à trois niveaux de raffinement.

---

## 6. En pratique {#pratique}

- Toujours **vérifier les bornes avant d'indexer** la grille — la première source de bugs.
- Initialiser les distances/coûts à une valeur « infinie » (`-1` pour BFS, `int.MaxValue` pour A\*)
  pour distinguer « non visité » de « visité à coût 0 ».
- Fixer un **ordre d'exploration** des voisins dès qu'on veut un chemin reproductible.

### Exercices du module

- **ex00-manhattan** — la distance de Manhattan (l'heuristique).
- **ex01-voisins** — énumérer les voisins libres d'une case.
- **ex02-bfs** — plus court chemin non pondéré (BFS).
- **ex03-astar** — coût minimal sur grille pondérée (A\*).
- **ex04-chemin** *(bonus)* — reconstruire et afficher le chemin.

## Références externes

- [Introduction to A* — Red Blob Games](https://www.redblobgames.com/pathfinding/a-star/introduction.html)
- [Queue\<T\> (doc Microsoft)](https://learn.microsoft.com/dotnet/api/system.collections.generic.queue-1)
- [PriorityQueue\<TElement,TPriority\> (doc Microsoft)](https://learn.microsoft.com/dotnet/api/system.collections.generic.priorityqueue-2)
