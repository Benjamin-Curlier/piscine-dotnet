# ex02-email — Email valide ?

## Objectif

Lis un entier **N**, puis **N** lignes. Pour chaque ligne, affiche `valide` si elle ressemble à
une **adresse email simple**, sinon `invalide` (une réponse par ligne).

Une adresse « simple » a la forme : quelque chose, un `@`, quelque chose, un point `.`, puis encore
quelque chose — sans `@` ni espace dans ces morceaux.

Exemple : `3` puis `user@mail.com`, `bad`, `a@b.c` → `valide`, `invalide`, `valide`.

## Livrable

- `Email.cs`

## Indices

- Importe `using System.Text.RegularExpressions;`.
- Motif : `@"^[^@\s]+@[^@\s]+\.[^@\s]+$"`.
  - `[^@\s]+` = un ou plusieurs caractères qui ne sont **ni** `@` **ni** un espace.
  - `@` puis `\.` (un vrai point) séparent les morceaux ; `^` et `$` encadrent toute la chaîne.
- `Regex.IsMatch(ligne, motif) ? "valide" : "invalide"` te donne la réponse.
