# ex01-table — Table de multiplication

## Objectif

Lis **un entier** `N` sur l'entrée standard, puis affiche sa **table de multiplication** de `1`
à `10`, une ligne par produit, au format exact `i x N = résultat`.

Exemple pour `2` :
```
1 x 2 = 2
2 x 2 = 4
...
10 x 2 = 20
```

## Livrable

- `Table.cs`

## Indices

- Une boucle `for (var i = 1; i <= 10; i++)`.
- L'**interpolation** de chaîne construit la ligne : `$"{i} x {n} = {i * n}"`.
- Respecte les espaces autour du `x` et du `=`.
