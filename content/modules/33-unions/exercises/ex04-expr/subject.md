# ex04-expr — Arbre d'expression (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Les unions peuvent être **récursives** : une variante peut contenir d'autres valeurs du même type.
C'est idéal pour un **arbre d'expression** arithmétique, qu'on évalue par pattern matching récursif.

Lis une expression en **notation polonaise préfixe** (l'opérateur précède ses opérandes) :
- un nombre est une feuille ;
- `+` ou `*` est suivi de **deux** sous-expressions.

Affiche la valeur évaluée.

### Comment lire la notation préfixe

En notation habituelle, l'opérateur se place **entre** ses deux opérandes : `3 + 4`. En notation
préfixe, il se place **avant** : `+ 3 4`. Chaque opérateur consomme toujours les deux expressions
qui le suivent ; une expression peut elle-même commencer par un opérateur.

Pour `+ 3 * 4 2`, lis de gauche à droite :

```text
+ 3 * 4 2
└─ gauche : 3
└─ droite : * 4 2  → 4 × 2
résultat : 3 + 8 = 11
```

Cette règle rend les parenthèses inutiles et se traduit directement par une fonction récursive.

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
