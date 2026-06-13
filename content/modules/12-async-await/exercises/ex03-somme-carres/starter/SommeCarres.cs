using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

// Lis N (première ligne), puis N entiers (un par ligne). Lance une tâche asynchrone par entier qui
// calcule son carré, attends-les TOUTES avec Task.WhenAll, puis affiche :
//   somme des carres = <somme des carrés>
// La sortie doit être DÉTERMINISTE (la somme ne dépend pas de l'ordre d'achèvement des tâches).

int n = int.Parse(System.Console.ReadLine());

// TODO : crée une List<Task<int>>, lance CarreAsync(x) pour chaque entier.
// TODO : int[] carres = await Task.WhenAll(taches);
// TODO : affiche la somme.

// TODO : static async Task<int> CarreAsync(int x) { await Task.Yield(); return x * x; }
