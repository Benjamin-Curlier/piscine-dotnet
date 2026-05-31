using System.Collections.Generic;

var dims = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var h = int.Parse(dims[0]);
var w = int.Parse(dims[1]);

var grille = new string[h];
for (var i = 0; i < h; i++)
{
    grille[i] = System.Console.ReadLine();
}

var depart = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var sr = int.Parse(depart[0]);
var sc = int.Parse(depart[1]);

var arrivee = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var er = int.Parse(arrivee[0]);
var ec = int.Parse(arrivee[1]);

var dr = new[] { -1, 1, 0, 0 };
var dc = new[] { 0, 0, -1, 1 };

int Heuristique(int r, int c) => System.Math.Abs(r - er) + System.Math.Abs(c - ec);

var cout = new int[h, w];
for (var i = 0; i < h; i++)
{
    for (var j = 0; j < w; j++)
    {
        cout[i, j] = int.MaxValue;
    }
}

// File de priorité A* : priorité = g + h (coût réel + estimation jusqu'à l'arrivée).
var ouverts = new PriorityQueue<(int r, int c), int>();
cout[sr, sc] = 0;
ouverts.Enqueue((sr, sc), Heuristique(sr, sc));

while (ouverts.Count > 0)
{
    var (r, c) = ouverts.Dequeue();
    if (r == er && c == ec) { break; }

    for (var d = 0; d < 4; d++)
    {
        var nr = r + dr[d];
        var nc = c + dc[d];
        if (nr < 0 || nr >= h || nc < 0 || nc >= w) { continue; }
        if (grille[nr][nc] == '#') { continue; }

        var coutEntree = grille[nr][nc] - '0';
        var nouveau = cout[r, c] + coutEntree;
        if (nouveau < cout[nr, nc])
        {
            cout[nr, nc] = nouveau;
            ouverts.Enqueue((nr, nc), nouveau + Heuristique(nr, nc));
        }
    }
}

if (cout[er, ec] == int.MaxValue)
{
    System.Console.WriteLine("IMPOSSIBLE");
}
else
{
    System.Console.WriteLine(cout[er, ec]);
}
