# ex02-age-valide — Age valide

## Objectif

Lis un entier **âge** (sur une ligne). Écris une méthode `void Valider(int age)` qui **lève** une
`ArgumentOutOfRangeException` si l'âge est `< 0` ou `> 150`.
Appelle `Valider` dans un `try/catch` : si tout va bien, affiche `Age: X` (où X est l'âge) ;
sinon, affiche `Age invalide`.

Exemple : `25` → `Age: 25`. Mais `-3` ou `200` → `Age invalide`.

## Livrable

- `Age.cs`

## Indices

- Ajoute `using System;` en haut (`ArgumentOutOfRangeException` est dans `System`).
- Lever une exception se fait avec `throw new ArgumentOutOfRangeException(nameof(age));`.
- En top-level, déclare la méthode locale `static void Valider(int age)` **en bas** du fichier.
- Entoure l'appel `Valider(age)` d'un `try { ... } catch (ArgumentOutOfRangeException) { ... }`.
