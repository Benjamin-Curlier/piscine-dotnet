# ex00-pairs-doubles — Pairs doublés

## Objectif

Lis **une ligne** d'entiers séparés par des espaces. Garde uniquement les nombres **pairs**,
**double** chacun d'eux, puis affiche le résultat sur **une seule ligne**, les valeurs séparées
par des espaces.

Exemple : `1 2 3 4 5 6` → les pairs sont `2 4 6`, doublés ils donnent `4 8 12`.

## Livrable

- `PairsDoubles.cs`

## Indices

- Ajoute `using System.Linq;` en haut (obligatoire pour `Where` et `Select`).
- Parse la ligne : `Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)`.
- Filtre avec `.Where(x => x % 2 == 0)`, transforme avec `.Select(x => x * 2)`.
- Affiche avec `System.Console.WriteLine(string.Join(" ", resultat))`.
