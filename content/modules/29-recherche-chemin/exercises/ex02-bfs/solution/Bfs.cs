using System.Collections.Generic;

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

var dr = new[] { -1, 1, 0, 0 };
var dc = new[] { 0, 0, -1, 1 };

var distance = new int[h, w];
for (var i = 0; i < h; i++)
{
    for (var j = 0; j < w; j++)
    {
        distance[i, j] = -1;
    }
}

var file = new Queue<(int r, int c)>();
file.Enqueue((sr, sc));
distance[sr, sc] = 0;

while (file.Count > 0)
{
    var (r, c) = file.Dequeue();
    for (var d = 0; d < 4; d++)
    {
        var nr = r + dr[d];
        var nc = c + dc[d];
        if (nr < 0 || nr >= h || nc < 0 || nc >= w) { continue; }
        if (grille[nr][nc] == '#') { continue; }
        if (distance[nr, nc] != -1) { continue; }
        distance[nr, nc] = distance[r, c] + 1;
        file.Enqueue((nr, nc));
    }
}

if (distance[er, ec] == -1)
{
    System.Console.WriteLine("IMPOSSIBLE");
}
else
{
    System.Console.WriteLine(distance[er, ec]);
}
