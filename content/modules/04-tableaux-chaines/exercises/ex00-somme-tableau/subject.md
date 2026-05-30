# ex00-somme-tableau — Somme d'un tableau

## Objectif

Lis **une ligne** contenant des entiers séparés par des **espaces**, puis affiche leur **somme**.

Exemples : `1 2 3 4` → `10` · `10 -2 5` → `13` · `42` → `42`.

## Livrable

- `SommeTableau.cs`

## Indices

- `ligne.Split(' ')` découpe la ligne en un **tableau** de morceaux.
- Parcours le tableau (`foreach`), convertis chaque morceau avec `int.Parse`, et accumule.
- Pour ignorer d'éventuels espaces en double : `Split(' ', System.StringSplitOptions.RemoveEmptyEntries)`.
