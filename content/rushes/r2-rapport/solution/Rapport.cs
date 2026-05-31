using System.Collections.Generic;
using System.Linq;

var n = int.Parse(System.Console.ReadLine());

var operations = new List<(string Categorie, int Montant)>();
for (var i = 0; i < n; i++)
{
    var morceaux = System.Console.ReadLine().Split(' ');
    operations.Add((morceaux[0], int.Parse(morceaux[1])));
}

var groupes = operations
    .GroupBy(o => o.Categorie)
    .OrderBy(g => g.Key);

foreach (var groupe in groupes)
{
    System.Console.WriteLine(groupe.Key + ": " + groupe.Sum(o => o.Montant));
}

System.Console.WriteLine("Total: " + operations.Sum(o => o.Montant));
