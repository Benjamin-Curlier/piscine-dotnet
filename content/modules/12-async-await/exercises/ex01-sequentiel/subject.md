# ex01-sequentiel — Carrés en séquence

## Objectif

Lis un entier **N**, puis **N** entiers. Pour chacun, **attends** le carré calculé par une
méthode asynchrone `CarreAsync` et affiche-le (un par ligne), **dans l'ordre de lecture**.

Ici on attend chaque tâche **une par une** : c'est l'exécution **séquentielle**.

Exemple : `3` puis `2`, `3`, `4` → `4`, `9`, `16`.

## Livrable

- `Sequentiel.cs`

## Indices

- Ajoute `using System.Threading.Tasks;`.
- Écris `static async Task<int> CarreAsync(int x) { await Task.Delay(1); return x * x; }`.
- Dans une boucle `for`, lis l'entier, puis `var c = await CarreAsync(n);` et affiche `c`.
- Comme tu fais `await` à chaque tour, les résultats sortent dans l'ordre, automatiquement.
