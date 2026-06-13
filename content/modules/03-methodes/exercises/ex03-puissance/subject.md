# ex03-puissance — Puissance récursive (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis une **base** (un entier, sur la première ligne) puis un **exposant** (un entier `>= 0`, sur la
seconde ligne). Calcule `base^exposant` à l'aide d'une **méthode récursive** et affiche le résultat.

Exemple : pour `2` puis `10`, le programme affiche `1024`.

## Livrable

- `Puissance.cs`

## Indices

- Le **cas de base** de la récursion : si l'exposant vaut `0`, le résultat est `1`.
- Le **cas récursif** : `base * Pow(base, exposant - 1)`.
- Lis la base avec `long.Parse(System.Console.ReadLine())` pour éviter les débordements sur les
  grandes puissances.
