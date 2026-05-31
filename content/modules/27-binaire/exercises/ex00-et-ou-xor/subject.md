# ex00-et-ou-xor — ET, OU, XOR

## Objectif

Lis deux entiers **a** puis **b** (un par ligne). Affiche sur **3 lignes** le résultat de chacun
des opérateurs bit à bit, dans cet ordre :

1. `a & b` (ET)
2. `a | b` (OU)
3. `a ^ b` (OU exclusif, XOR)

Exemple : `6` puis `3` → `2`, `7`, `5`.

## Livrable

- `EtOuXor.cs`

## Indices

- `&`, `|` et `^` comparent les deux nombres **bit par bit**.
- `6` vaut `110` en binaire et `3` vaut `011` : `6 & 3 = 010 = 2`, `6 | 3 = 111 = 7`,
  `6 ^ 3 = 101 = 5`.
- Lis chaque entier avec `int.Parse(System.Console.ReadLine())` et affiche chaque résultat sur sa
  propre ligne avec `System.Console.WriteLine(...)`.
