# ex00-singleton — Singleton

## Objectif

Implémente le patron **Singleton** : une classe qui n'a **qu'une seule instance**, partagée par
tout le programme via un point d'accès global.

Lis deux entiers `a` et `b` (un par ligne). Imagine deux services différents qui incrémentent le
**même** compteur : le premier `a` fois, le second `b` fois. Affiche la valeur finale du compteur
(donc `a + b`).

Exemple : `3` puis `5` → `8`.

## Livrable

- `Compteur.cs`

## Contraintes

- Le `Compteur` doit avoir un **constructeur privé** : impossible de faire `new Compteur()` ailleurs.
- L'accès se fait uniquement via une propriété statique `Instance`.

## Indices

- Champ statique privé `_instance` + propriété `public static Compteur Instance => _instance ??= new Compteur();`
  (`??=` crée l'objet au premier accès, puis renvoie toujours le même).
- La `Valeur` a un setter privé ; seule la méthode `Incrementer()` la modifie.
- Comme les deux boucles passent par `Compteur.Instance`, elles agissent sur le même objet — c'est
  tout l'intérêt du singleton.
