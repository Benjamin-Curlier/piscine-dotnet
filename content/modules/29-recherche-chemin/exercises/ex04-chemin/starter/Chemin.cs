using System.Collections.Generic;
using System.Text;

// Lis "H W" puis H lignes de grille (S = départ, E = arrivée, '#' = mur, '.' = libre).
// Affiche le plus court chemin de S à E comme une suite de lettres U/D/L/R, ou IMPOSSIBLE.
// IMPORTANT : explore les voisins dans l'ordre Haut(U), Bas(D), Gauche(L), Droite(R)
// pour que le chemin reconstruit soit déterministe.

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

// TODO : BFS en mémorisant pour chaque case son prédécesseur + la lettre du déplacement,
//        puis remonte de E à S et inverse la séquence.
