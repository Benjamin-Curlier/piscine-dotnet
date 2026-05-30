# ex00-division-securisee — Division sécurisée

## Objectif

Lis deux entiers : **a** (première ligne), puis **b** (deuxième ligne).
Affiche le résultat de la **division entière** `a / b`.
Si `b` vaut `0`, la division lève une `DivideByZeroException` : attrape-la et affiche à la place
`Erreur: division par zero`.

Exemple : `10` puis `2` → `5`. Mais `7` puis `0` → `Erreur: division par zero`.

## Livrable

- `Division.cs`

## Indices

- Les exceptions vivent dans l'espace de noms `System` : ajoute `using System;` en haut.
- Mets le calcul dans un bloc `try { ... }` puis attrape l'erreur avec `catch (DivideByZeroException) { ... }`.
- La division entre deux `int` est une **division entière** : `9 / 3` donne `3`, pas `3,0`.
