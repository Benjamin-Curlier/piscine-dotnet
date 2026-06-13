# ex03-fibonacci — Suite de Fibonacci (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis un entier `N` et affiche le **N-ième terme** de la suite de Fibonacci, indexée à partir de 0 :

- `F(0) = 0`
- `F(1) = 1`
- `F(n) = F(n-1) + F(n-2)`

Exemple : pour `7`, le programme affiche `13` (suite : 0, 1, 1, 2, 3, 5, 8, **13**).

## Livrable

- `Fibonacci.cs`

## Indices

- Pas besoin de récursion : deux variables suffisent. Initialise `a = 0` et `b = 1`.
- À chaque tour : `tmp = a + b`, puis `a = b`, puis `b = tmp`.
- Après `N` tours de boucle, `a` contient `F(N)`. Vérifie les cas limites `F(0) = 0` et `F(1) = 1`.
