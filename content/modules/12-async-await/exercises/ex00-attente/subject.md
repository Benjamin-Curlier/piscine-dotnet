# ex00-attente — Doubler en asynchrone

## Objectif

Lis un entier **N**. Écris une méthode **asynchrone** `DoublerAsync` qui attend brièvement
(`await Task.Delay(1);`) puis renvoie le double de son argument. Dans le programme, **attends**
le résultat avec `await` et affiche-le.

Exemple : `5` → `10`. `0` → `0`. `7` → `14`.

## Livrable

- `Attente.cs`

## Indices

- Ajoute `using System.Threading.Tasks;` (nécessaire pour `Task` et `Task.Delay`).
- La méthode renvoie un `Task<int>` : `static async Task<int> DoublerAsync(int x)`.
- Dans le corps : `await Task.Delay(1);` puis `return x * 2;`.
- Au niveau du programme, tu peux écrire `await DoublerAsync(n)` directement (top-level await).
