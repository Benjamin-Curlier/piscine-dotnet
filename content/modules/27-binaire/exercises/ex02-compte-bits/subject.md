# ex02-compte-bits — Compter les bits à 1

## Objectif

Lis un entier **n** (positif ou nul). Affiche le **nombre de bits à 1** dans sa représentation
binaire.

Exemple : `7` vaut `111` en binaire → `3` bits à 1.

## Livrable

- `CompteBits.cs`

## Contrainte

Implémente le comptage **à la main** : ne te sers pas de `BitOperations.PopCount`.

## Indices

- Le **masque** `n & 1` isole le bit de poids faible : il vaut `1` si ce bit est à 1, sinon `0`.
- Tant que `n` n'est pas nul : ajoute `n & 1` à un compteur, puis décale `n` d'un bit vers la
  droite avec `n >>= 1`.
- `255` vaut `11111111` : huit bits à 1.
