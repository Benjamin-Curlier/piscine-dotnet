# ex02-recherche-binaire — Recherche binaire

## Objectif

Lis :
1. Un entier **N** (nombre d'éléments),
2. **N entiers séparés par des espaces**, déjà triés par ordre croissant,
3. Une **cible** (entier).

Affiche l'**indice 0-based** de la cible dans le tableau, ou **`-1`** si elle est absente.
Implémente la **recherche binaire** à la main.

Exemples :
- `5`, `1 2 3 4 5`, cible `3` → `2`
- `5`, `1 2 3 4 5`, cible `6` → `-1`
- `3`, `10 20 30`, cible `10` → `0`

## Livrable

- `RechercheBinaire.cs`

## Contraintes

- **Interdit** : `Array.BinarySearch`, `Array.IndexOf`, LINQ `.IndexOf()` ou tout ce qui effectue
  la recherche à ta place.
- Le tableau est **garanti trié** en entrée : tu n'as pas à le trier.

## Indices

- Initialise `gauche = 0` et `droite = n - 1`.
- Dans une boucle `while (gauche <= droite)` :
  - Calcule `milieu = (gauche + droite) / 2`.
  - Si `t[milieu] == cible` : tu as trouvé → affiche `milieu` et termine.
  - Si `t[milieu] < cible` : la cible est à droite → `gauche = milieu + 1`.
  - Sinon : la cible est à gauche → `droite = milieu - 1`.
- Si la boucle se termine sans trouver, affiche `-1`.
