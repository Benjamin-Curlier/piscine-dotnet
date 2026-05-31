# ex04-expr — Arbre d'expression (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Les unions peuvent être **récursives** : une variante peut contenir d'autres valeurs du même type.
C'est idéal pour un **arbre d'expression** arithmétique, qu'on évalue par pattern matching récursif.

Lis une expression en **notation polonaise préfixe** (l'opérateur précède ses opérandes) :
- un nombre est une feuille ;
- `+` ou `*` est suivi de **deux** sous-expressions.

Affiche la valeur évaluée.

Exemples :
- `+ 3 * 4 2` → `11` (soit `3 + (4 × 2)`) ;
- `* + 1 2 3` → `9` (soit `(1 + 2) × 3`) ;
- `5` → `5`.

## Livrable

- `Expression.cs`

## Contraintes

- Union récursive : `Operation` contient deux `Expr`.
- Lecture **et** évaluation récursives ; pas de pile explicite nécessaire.

## Indices

- `sealed record Nombre(int Valeur) : Expr;` et `sealed record Operation(string Symbole, Expr Gauche, Expr Droite) : Expr;`.
- Lis avec un index partagé (`ref int pos`) : si le token est `+`/`*`, lis deux sous-arbres ;
  sinon, c'est un `Nombre`.
- Évalue par un `switch` récursif : `Operation o when o.Symbole == "+" => Evaluer(o.Gauche) + Evaluer(o.Droite)`, etc.
