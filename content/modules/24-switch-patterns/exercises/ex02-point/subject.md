# ex02-point — Classer un point

## Objectif

Lis deux entiers : `X` puis `Y` (un par ligne). Construis un `Point(X, Y)` et **classe-le** :

- `X == 0` **et** `Y == 0` → `origine`
- `X == 0` (mais pas Y) → `axe Y`
- `Y == 0` (mais pas X) → `axe X`
- sinon → `quelconque`

Exemple : `(0, 0)` → `origine` ; `(0, 5)` → `axe Y` ; `(2, 3)` → `quelconque`.

## Livrable

- `Point.cs`

## Indices

- Déclare un **record** : `record Point(int X, int Y);` (en bas du fichier, après les
  instructions).
- Un **property pattern** teste les propriétés : `{ X: 0, Y: 0 }`, `{ X: 0 }`, `{ Y: 0 }`.
- Place le cas le **plus précis** en premier (`origine`), sinon `{ X: 0 }` capterait aussi
  l'origine.
- `p switch { { X: 0, Y: 0 } => "origine", { X: 0 } => "axe Y", { Y: 0 } => "axe X", _ => "quelconque" }`.
