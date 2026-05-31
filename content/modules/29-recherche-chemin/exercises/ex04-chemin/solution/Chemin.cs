using System.Collections.Generic;
using System.Text;

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

var (sr, sc, er, ec) = (-1, -1, -1, -1);
for (var i = 0; i < h; i++)
{
    for (var j = 0; j < w; j++)
    {
        if (grille[i][j] == 'S') { sr = i; sc = j; }
        if (grille[i][j] == 'E') { er = i; ec = j; }
    }
}

// Ordre fixe : Haut, Bas, Gauche, Droite — pour un chemin reconstruit déterministe.
var dr = new[] { -1, 1, 0, 0 };
var dc = new[] { 0, 0, -1, 1 };
var lettre = new[] { 'U', 'D', 'L', 'R' };

var precedent = new (int r, int c, char move)[h, w];
var vu = new bool[h, w];

var file = new Queue<(int r, int c)>();
file.Enqueue((sr, sc));
vu[sr, sc] = true;

while (file.Count > 0)
{
    var (r, c) = file.Dequeue();
    for (var d = 0; d < 4; d++)
    {
        var nr = r + dr[d];
        var nc = c + dc[d];
        if (nr < 0 || nr >= h || nc < 0 || nc >= w) { continue; }
        if (grille[nr][nc] == '#') { continue; }
        if (vu[nr, nc]) { continue; }
        vu[nr, nc] = true;
        precedent[nr, nc] = (r, c, lettre[d]);
        file.Enqueue((nr, nc));
    }
}

if (!vu[er, ec])
{
    System.Console.WriteLine("IMPOSSIBLE");
    return;
}

var moves = new List<char>();
var (cr, cc) = (er, ec);
while (cr != sr || cc != sc)
{
    var p = precedent[cr, cc];
    moves.Add(p.move);
    cr = p.r;
    cc = p.c;
}

var sb = new StringBuilder();
for (var i = moves.Count - 1; i >= 0; i--)
{
    sb.Append(moves[i]);
}

System.Console.WriteLine(sb.ToString());
