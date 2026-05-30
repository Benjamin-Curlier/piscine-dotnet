using System;

Func<int, bool> estPair = x => x % 2 == 0;

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var valeur = int.Parse(System.Console.ReadLine());
    if (estPair(valeur))
    {
        System.Console.WriteLine(valeur);
    }
}
