using System;

Func<int, int> carre = x => x * x;

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var valeur = int.Parse(System.Console.ReadLine());
    System.Console.WriteLine(carre(valeur));
}
