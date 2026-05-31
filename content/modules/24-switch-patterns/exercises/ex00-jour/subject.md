# ex00-jour — Le nom du jour

## Objectif

Lis un entier **n** entre `1` et `7` et affiche le nom du jour correspondant :
`1` → `lundi`, `2` → `mardi`, ..., `7` → `dimanche`. Pour toute autre valeur, affiche
`inconnu`.

Exemple : `1` → `lundi` ; `6` → `samedi` ; `9` → `inconnu`.

## Livrable

- `Jour.cs`

## Indices

- Une **switch expression** renvoie une valeur : `var nom = n switch { ... };`.
- Chaque branche utilise une flèche : `1 => "lundi"`.
- Le motif **`_`** (joker) attrape tous les autres cas : `_ => "inconnu"`.
- Lis l'entier avec `int.Parse(System.Console.ReadLine())`.
