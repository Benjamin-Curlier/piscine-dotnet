var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

var cible = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var r = int.Parse(cible[0]);
var c = int.Parse(cible[1]);

// Ordre fixe : Haut, Bas, Gauche, Droite.
var dr = new[] { -1, 1, 0, 0 };
var dc = new[] { 0, 0, -1, 1 };

var trouve = false;
for (var d = 0; d < 4; d++)
{
    var nr = r + dr[d];
    var nc = c + dc[d];
    if (nr >= 0 && nr < h && nc >= 0 && nc < w && grille[nr][nc] == '.')
    {
        System.Console.WriteLine(nr + " " + nc);
        trouve = true;
    }
}

if (!trouve)
{
    System.Console.WriteLine("AUCUN");
}
