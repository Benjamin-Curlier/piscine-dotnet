// Ce code FONCTIONNE mais le calcul de valeur absolue est dupliqué.
// Refactore-le : extrais une méthode Distance(x, y). Les tests doivent rester verts.

var v = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var a = int.Parse(v[0]);
var b = int.Parse(v[1]);
var c = int.Parse(v[2]);
var d = int.Parse(v[3]);

int d1;
if (a - b < 0)
{
    d1 = b - a;
}
else
{
    d1 = a - b;
}

int d2;
if (c - d < 0)
{
    d2 = d - c;
}
else
{
    d2 = c - d;
}

System.Console.WriteLine(d1 + d2);
