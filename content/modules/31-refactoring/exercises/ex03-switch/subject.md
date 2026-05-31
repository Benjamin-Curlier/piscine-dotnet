# ex03-switch — Chaîne de if → switch expression

## Objectif

Le code fourni enchaîne `if / else if / else` pour choisir une opération. Refactore-le en un
**switch expression**, plus court et plus lisible, sans changer le comportement.

Le programme lit une ligne `op a b` (où `op` ∈ `add`, `sub`, `mul`) et affiche le résultat.

Exemples : `add 3 4` → `7` ; `mul 5 5` → `25`.

## Livrable

- `Operation.cs`

## Contraintes

- Comportement identique (tests `io` = filet de régression).
- Remplace la cascade de `if` par un `switch` expression avec un cas par défaut `_`.

## Indices

- `var resultat = op switch { "add" => a + b, "sub" => a - b, "mul" => a * b, _ => 0 };`.
- Le `switch` expression rend l'intention (« à chaque opérande son calcul ») visible d'un coup d'œil.
