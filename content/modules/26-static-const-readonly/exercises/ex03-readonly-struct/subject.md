# ex03-readonly-struct — Norme 1 d'un `readonly struct` *(bonus)*

## Objectif

Définis un **`readonly struct`** `Vecteur` avec deux paramètres entiers `x` et `y`.
Ajoute une propriété calculée `Norme1` qui retourne la **norme 1** : `|x| + |y|`
(somme des valeurs absolues).

Lis **X** puis **Y** sur l'entrée standard, crée un `Vecteur`, et affiche sa `Norme1`.

Exemples :
- Entrée `3`, `4` → sortie `7`
- Entrée `-2`, `5` → sortie `7`
- Entrée `0`, `0` → sortie `0`

## Livrable

- `ReadonlyStruct.cs`

## Indices

- Ajoute `using System;` en haut pour utiliser `Math.Abs`.
- Syntaxe du constructeur primaire : `readonly struct Vecteur(int x, int y) { ... }`.
- Propriété calculée : `public int Norme1 => Math.Abs(x) + Math.Abs(y);`.
- Le `readonly` garantit qu'aucune méthode du struct ne modifie ses champs.
- Déclare le struct **après** le code principal (top-level).
