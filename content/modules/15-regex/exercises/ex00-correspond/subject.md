# ex00-correspond — Que des chiffres ?

## Objectif

Lis un entier **N**, puis **N** lignes de texte. Pour chaque ligne, affiche `oui` si elle ne
contient **que des chiffres**, sinon `non` (une réponse par ligne).

Exemple : `3` puis `123`, `abc`, `45a` → `oui`, `non`, `non` (`abc` n'a pas de chiffre, `45a`
contient une lettre).

## Livrable

- `Correspond.cs`

## Indices

- Importe `using System.Text.RegularExpressions;`.
- Le motif `@"^\d+$"` signifie : du début (`^`) à la fin (`$`), uniquement des chiffres (`\d+`).
- `Regex.IsMatch(ligne, @"^\d+$") ? "oui" : "non"` te donne directement la réponse à afficher.
- Lis l'entier avec `int.Parse(System.Console.ReadLine())`, puis boucle `N` fois.
