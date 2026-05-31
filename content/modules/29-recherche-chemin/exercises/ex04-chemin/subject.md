# ex04-chemin — Reconstruction du chemin (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Comme l'ex02, mais au lieu du *nombre* de pas, affiche le **chemin lui-même** : la suite des
déplacements de `S` à `E`, codés `U` (haut), `D` (bas), `L` (gauche), `R` (droite).

Entrée : identique à l'ex02 (`H W`, puis la grille `S`/`E`/`#`/`.`).

Affiche la chaîne des déplacements (sans espaces), ou `IMPOSSIBLE` si `E` est inaccessible.

Exemple :
```
3 3
S..
.#.
..E
```
→ `DDRR`

## Livrable

- `Chemin.cs`

## Contraintes

- **Ordre d'exploration imposé** : Haut, Bas, Gauche, Droite. C'est ce qui rend le chemin
  reconstruit **unique** (donc vérifiable) quand plusieurs plus courts chemins existent.

## Indices

- Fais un BFS comme à l'ex02, mais mémorise pour chaque case **d'où** tu y es arrivé : son
  prédécesseur `(r, c)` et la **lettre** du déplacement qui y mène.
- La lettre correspond à la direction utilisée : Haut → `U`, Bas → `D`, Gauche → `L`, Droite → `R`.
- Une fois `E` atteint, remonte de prédécesseur en prédécesseur jusqu'à `S` en empilant les
  lettres, puis **inverse** la séquence (un `System.Text.StringBuilder` parcouru à l'envers fait l'affaire).
