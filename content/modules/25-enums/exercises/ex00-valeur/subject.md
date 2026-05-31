# ex00-valeur — La valeur d'une couleur

## Objectif

Lis un **nom de couleur** sur une ligne (`Rouge`, `Vert` ou `Bleu`) et affiche sa **valeur
entière** sous-jacente.

Une énumération `Couleur { Rouge, Vert, Bleu }` numérote ses membres à partir de `0` :
`Rouge` vaut `0`, `Vert` vaut `1`, `Bleu` vaut `2`.

Exemple : `Rouge` → `0`, `Vert` → `1`, `Bleu` → `2`.

## Livrable

- `Valeur.cs`

## Indices

- Déclare `enum Couleur { Rouge, Vert, Bleu }`.
- Convertis le texte lu en valeur d'enum avec `Enum.Parse<Couleur>(nom)`.
- Pour obtenir le nombre derrière le membre, fais un **cast** : `(int)c`.
- `Enum.Parse` exige `using System;`.
