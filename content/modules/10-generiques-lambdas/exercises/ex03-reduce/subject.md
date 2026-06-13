# ex03-reduce — Réduction générique (bonus)

> **Bonus difficile, non bloquant.** Synthèse génériques + lambdas : une seule fonction générique
> `Reduce` (fold) réutilisée avec deux lambdas différentes.

## Énoncé

Lis des entiers (**un par ligne**) jusqu'à la fin de l'entrée, puis affiche :

```
somme=<somme des entiers>
produit=<produit des entiers>
```

**Contrainte** : calcule la somme **et** le produit avec **une seule** méthode générique
`Reduce<T, R>(IEnumerable<T> source, R seed, Func<R, T, R> combiner)` (un *fold* / réduction),
appelée avec deux lambdas distinctes. La somme part de `0`, le produit de `1`.

## Exemple

```
Entrée :
2
3
4

Sortie :
somme=9
produit=24
```

## Indications

- `Reduce` part de `seed`, puis pour chaque élément fait `acc = combiner(acc, element)`.
- `Func<R, T, R>` est une fonction qui prend l'accumulateur (`R`) et un élément (`T`) et renvoie le
  nouvel accumulateur (`R`).
- Les deux appels ne diffèrent que par le `seed` (`0` / `1`) et la lambda (`(a, x) => a + x` /
  `(a, x) => a * x`).
