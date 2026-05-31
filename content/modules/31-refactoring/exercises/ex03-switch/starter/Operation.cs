// Ce code FONCTIONNE mais la cascade de if/else if s'allonge à chaque opération.
// Refactore-le en switch expression. Les tests doivent rester verts.

var v = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var op = v[0];
var a = int.Parse(v[1]);
var b = int.Parse(v[2]);

int resultat;
if (op == "add")
{
    resultat = a + b;
}
else if (op == "sub")
{
    resultat = a - b;
}
else if (op == "mul")
{
    resultat = a * b;
}
else
{
    resultat = 0;
}

System.Console.WriteLine(resultat);
