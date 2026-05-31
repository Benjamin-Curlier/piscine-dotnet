# ex03-tri-fusion — Tri fusion *(bonus)*

## Objectif

Lis un entier **N** (nombre d'éléments), puis **N entiers séparés par des espaces** sur la
deuxième ligne. Trie le tableau par ordre **croissant** en utilisant le **tri fusion récursif**
implémenté à la main. Affiche le résultat : les entiers séparés par un espace.

Exemples :
- `5`, `5 3 1 4 2` → `1 2 3 4 5`
- `6`, `6 5 4 3 2 1` → `1 2 3 4 5 6`
- `1`, `9` → `9`

## Livrable

- `TriFusion.cs`

## Contraintes

- **Interdit** : `Array.Sort`, `List.Sort`, LINQ `.OrderBy()` ou tout tri de la bibliothèque standard.
- L'algorithme doit être **récursif** et implémenter le principe **diviser-pour-régner**.

## Indices

- Écris une méthode locale `static int[] TriFusion(int[] t)` :
  - **Cas de base** : si `t.Length <= 1`, retourne `t` tel quel.
  - **Division** : coupe en deux moitiés avec `System.Array.Copy`.
  - **Récursion** : appelle `TriFusion` sur chaque moitié.
  - **Fusion** : appelle une méthode `Fusionner(gauche, droite)`.
- `Fusionner(int[] g, int[] d)` :
  - Crée un tableau `resultat` de taille `g.Length + d.Length`.
  - Parcours avec trois indices `i`, `j`, `k` ; compare `g[i]` et `d[j]`, prend le plus petit.
  - Copie les restes éventuels de chaque moitié.
- Les méthodes locales (`static`) se déclarent **après** le code top-level.
