# ex02-whenall — Carrés en parallèle (WhenAll)

## Objectif

Lis un entier **N**, puis **N** entiers. Cette fois, **lance** les N calculs `CarreAsync`
**sans les attendre un par un** : range les `Task<int>` dans un tableau. Ensuite, attends-les
**toutes ensemble** avec `Task.WhenAll`, qui renvoie un tableau de résultats **dans l'ordre**.
Affiche chaque résultat (un par ligne).

Exemple : `3` puis `2`, `3`, `4` → `4`, `9`, `16`.

## Livrable

- `WhenAll.cs`

## Indices

- Ajoute `using System.Threading.Tasks;`.
- Crée `var tasks = new Task<int>[n];` et remplis-le dans la boucle : `tasks[i] = CarreAsync(valeur);`
  (note : **pas** de `await` ici, on récupère juste la tâche).
- Puis `var resultats = await Task.WhenAll(tasks);` : `resultats` est un `int[]` ordonné.
- Affiche chaque élément de `resultats` dans l'ordre. Pas besoin de LINQ.
