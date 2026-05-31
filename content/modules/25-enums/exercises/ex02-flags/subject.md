# ex02-flags — Combiner des permissions

## Objectif

Lis un entier **N**, puis **N** lignes contenant chacune un nom de permission (`Lecture`,
`Ecriture` ou `Execution`). **Combine** toutes ces permissions et affiche la **valeur entière**
résultante.

L'enum est marqué `[Flags]` et ses membres valent des **puissances de 2** :
`Lecture = 1`, `Ecriture = 2`, `Execution = 4`. Combiner `Lecture` et `Ecriture` donne `3`.

Exemple : `2` puis `Lecture`, `Ecriture` → `3`.

## Livrable

- `Flags.cs`

## Indices

- Déclare `[Flags] enum Permissions { Lecture = 1, Ecriture = 2, Execution = 4 }`.
- Pars d'un accumulateur vide (`Permissions resultat = default;`).
- Pour chaque nom lu, ajoute le drapeau avec l'opérateur OU binaire :
  `resultat |= Enum.Parse<Permissions>(nom);`.
- Affiche enfin `(int)resultat`.
- `Enum.Parse` exige `using System;`.
