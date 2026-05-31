# ex02-guard-clauses — Conditions imbriquées → clauses-gardes

## Objectif

Le code fourni empile des `if` les uns dans les autres (« code en escalier »), ce qui rend la
logique difficile à suivre. Refactore-le avec des **clauses-gardes** : des retours anticipés qui
traitent les cas d'exclusion en premier.

Le programme lit un `age` puis un `solde`, et affiche `OUI` si la personne est éligible, sinon
`NON`. Éligible = `age >= 18` **et** `0 <= solde <= 100000`.

Exemples : `20` / `500` → `OUI` ; `16` / `500` → `NON`.

## Livrable

- `Eligibilite.cs`

## Contraintes

- Comportement identique (tests `io` = filet de régression).
- Aplatis les `if` imbriqués en retours anticipés dans une méthode `EstEligible`.

## Indices

- `if (age < 18) { return false; }` puis les autres exclusions, et `return true;` à la fin.
- Les clauses-gardes réduisent l'indentation et rendent les conditions d'échec explicites.
