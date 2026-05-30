# ex01-max3 — Maximum de trois

## Objectif

Lis **trois entiers** (un par ligne) sur l'entrée standard, puis affiche le **plus grand** des trois.

Le but est d'écrire **une seule** méthode `Max(int x, int y)` et de la **réutiliser** pour traiter
les trois valeurs.

Exemples : `3, 9, 5` → `9` · `10, 2, 7` → `10` · `-4, -9, -1` → `-1`.

## Livrable

- `Max3.cs`

## Indices

- Écris `static int Max(int x, int y) => x > y ? x : y;`.
- Combine les appels : `Max(a, Max(b, c))`.
