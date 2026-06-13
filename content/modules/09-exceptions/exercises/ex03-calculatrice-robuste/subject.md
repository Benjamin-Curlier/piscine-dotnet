# ex03-calculatrice-robuste — Calculatrice tolérante aux erreurs (bonus)

> **Bonus difficile, non bloquant.** Synthèse de la gestion d'exceptions : plusieurs types
> d'erreurs, traités **par ligne**, sans interrompre le reste.

## Énoncé

Lis un entier **N** sur la première ligne, puis **N lignes** de la forme `a op b` (trois éléments
séparés par une espace ; `op` ∈ `+`, `-`, `*`, `/`, `a` et `b` entiers).

Pour chaque ligne `i` (de 1 à N), affiche :

- `i: <résultat>` si le calcul réussit ;
- `i: erreur de format` si `a` ou `b` n'est pas un entier valide (`FormatException`) ;
- `i: division par zero` en cas de division par zéro (`DivideByZeroException`) ;
- `i: operateur inconnu` si `op` n'est pas un opérateur reconnu.

Une erreur sur une ligne **ne doit pas** interrompre le traitement des lignes suivantes.

## Exemple

```
Entrée :
3
10 + 5
8 / 0
4 x 2

Sortie :
1: 15
2: division par zero
3: operateur inconnu
```

## Indications

- Place le calcul d'une ligne dans un `try`, avec un `catch` **par type d'exception**.
- L'ordre des `catch` (du plus précis au plus général) n'a pas d'importance ici car les types sont
  disjoints — mais n'attrape jamais `Exception` « pour tout avaler ».
- Un opérateur inconnu peut être signalé en levant toi-même une exception dans le `switch`.
