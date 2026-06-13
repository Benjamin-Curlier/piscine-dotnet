# ex03-somme-carres — Somme des carrés en parallèle (bonus)

> **Bonus difficile, non bloquant.** Synthèse async/await : lancer plusieurs tâches et les agréger
> avec `Task.WhenAll`.

## Énoncé

Lis **N** (première ligne), puis **N entiers** (un par ligne). Lance **une tâche asynchrone par
entier** qui calcule son carré, attends-les **toutes** avec `Task.WhenAll`, puis affiche :

```
somme des carres = <somme des carrés>
```

La sortie doit être **déterministe** : la somme ne dépend pas de l'ordre d'achèvement des tâches.

## Exemple

```
Entrée :
3
2
3
4

Sortie :
somme des carres = 29
```

(`2² + 3² + 4² = 4 + 9 + 16 = 29`)

## Indications

- Stocke les tâches dans une `List<Task<int>>`, puis `int[] carres = await Task.WhenAll(taches);`.
- `Task.WhenAll` renvoie les résultats **dans l'ordre des tâches** ; mais comme on fait juste une
  somme, l'ordre n'a de toute façon pas d'importance — d'où le déterminisme.
- Une méthode `async Task<int>` peut faire `await Task.Yield();` pour être réellement asynchrone
  sans délai, puis `return x * x;`.
- Le programme utilise un `await` au niveau supérieur (top-level `await`).
