// Lis "H W", puis H lignes de grille ('.' = libre, '#' = mur), puis une case "r c".
// Affiche les voisins LIBRES dans l'ordre Haut, Bas, Gauche, Droite, un "r c" par ligne.
// Si aucun voisin libre, affiche AUCUN.

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

// TODO : lis la case cible, teste les 4 voisins dans l'ordre, affiche ceux qui valent '.'.
