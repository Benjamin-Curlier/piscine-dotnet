# ex01-couleur-hex — Couleur en hexadécimal

## Objectif

Lis un **nom de couleur** sur une ligne (`Rouge`, `Vert` ou `Bleu`) et affiche son **code
hexadécimal** :

- `Rouge` → `#FF0000`
- `Vert` → `#00FF00`
- `Bleu` → `#0000FF`

## Livrable

- `CouleurHex.cs`

## Indices

- Déclare `enum Couleur { Rouge, Vert, Bleu }`.
- Transforme le texte lu avec `Enum.Parse<Couleur>(nom)`.
- Choisis le code avec une **switch expression** sur la valeur d'enum :
  `couleur switch { Couleur.Rouge => "#FF0000", ... }`.
- `Enum.Parse` exige `using System;`.
