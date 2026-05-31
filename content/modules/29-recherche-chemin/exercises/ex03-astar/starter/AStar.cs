using System.Collections.Generic;

// Lis "H W", H lignes de grille (chiffres 1-9 = coût d'entrée dans la case, '#' = mur),
// puis "rS cS" (départ) et "rE cE" (arrivée).
// Affiche le coût total minimal d'un chemin de départ à arrivée (A*), ou IMPOSSIBLE.
// Le départ ne coûte rien ; entrer dans une case ajoute son chiffre.
// Indice : System.Collections.Generic.PriorityQueue, priorité = g + heuristique de Manhattan.

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

// TODO : lis départ/arrivée, fais un A* avec heuristique de Manhattan, affiche le coût ou IMPOSSIBLE.
