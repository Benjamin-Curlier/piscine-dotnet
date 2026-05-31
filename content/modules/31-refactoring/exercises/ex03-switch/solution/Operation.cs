// Version refactorée : la chaîne de if/else if devient un switch expression concis.
var v = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var op = v[0];
var a = int.Parse(v[1]);
var b = int.Parse(v[2]);

var resultat = op switch
{
    "add" => a + b,
    "sub" => a - b,
    "mul" => a * b,
    _ => 0
};

System.Console.WriteLine(resultat);
