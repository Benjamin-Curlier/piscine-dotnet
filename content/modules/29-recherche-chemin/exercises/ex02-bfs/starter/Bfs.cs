using System.Collections.Generic;

// Lis "H W" puis H lignes de grille (S = départ, E = arrivée, '#' = mur, '.' = libre).
// Affiche le nombre minimal de pas de S à E (déplacements 4 directions), ou IMPOSSIBLE.
// Indice : parcours en largeur (BFS) avec une Queue et un tableau distance initialisé à -1.

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

// TODO : repère S et E, fais un BFS, affiche distance[E] ou IMPOSSIBLE.
