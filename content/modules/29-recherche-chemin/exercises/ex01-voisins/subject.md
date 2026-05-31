# ex01-voisins — Voisins libres sur une grille

## Objectif

Lis une grille ASCII et une case, puis affiche les **cases voisines libres** accessibles en un
pas (4 directions, pas de diagonale).

Entrée :
- une ligne `H W` (hauteur = nombre de lignes, largeur = nombre de colonnes) ;
- `H` lignes de grille : `.` = case libre, `#` = mur ;
- une ligne `r c` : la case dont on veut les voisins (ligne `r`, colonne `c`, indexées à partir de 0).

Affiche, **dans l'ordre Haut, Bas, Gauche, Droite**, chaque voisin libre sous la forme `r c`
(un par ligne). Si aucun voisin n'est libre, affiche `AUCUN`.

Exemple :
```
3 4
....
.#..
....
1 2
```
→
```
0 2
2 2
1 3
```
(le voisin de gauche `1 1` est un mur, il est ignoré.)

## Livrable

- `Voisins.cs`

## Contraintes

- Respecte l'ordre Haut → Bas → Gauche → Droite.
- Ignore les voisins hors de la grille.

## Indices

- Haut = `(r-1, c)`, Bas = `(r+1, c)`, Gauche = `(r, c-1)`, Droite = `(r, c+1)`.
- Stocke les décalages dans deux tableaux `dr = {-1, 1, 0, 0}` et `dc = {0, 0, -1, 1}` et boucle.
- Vérifie d'abord que le voisin est dans les bornes (`0 <= nr < H` et `0 <= nc < W`) avant de
  lire `grille[nr][nc]`.
