# ex01-stats — Statistiques

## Objectif

Lis **une ligne** d'entiers séparés par des espaces. Affiche **quatre lignes** calculées avec les
agrégats LINQ :

```
Somme: <somme>
Min: <minimum>
Max: <maximum>
Moyenne: <moyenne entière>
```

La moyenne est **tronquée à l'entier** (la partie décimale est coupée). Par exemple, pour
`1 2 3 4`, la moyenne `10 / 4 = 2.5` s'affiche `2`.

Exemple : `1 2 3 4` → `Somme: 10`, `Min: 1`, `Max: 4`, `Moyenne: 2`.

## Livrable

- `Stats.cs`

## Indices

- Ajoute `using System.Linq;` en haut.
- Parse la ligne en tableau : `Split(...).Select(int.Parse).ToArray()`.
- Utilise `.Sum()`, `.Min()`, `.Max()` ; pour la moyenne tronquée : `(int)nombres.Average()`.
- Affiche chaque ligne avec une chaîne interpolée, par exemple `$"Somme: {nombres.Sum()}"`.
