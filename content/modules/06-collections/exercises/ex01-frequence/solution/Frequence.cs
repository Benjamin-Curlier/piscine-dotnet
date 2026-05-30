using System.Collections.Generic;

var mots = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var cible = System.Console.ReadLine();

var frequences = new Dictionary<string, int>();
foreach (var mot in mots)
{
    frequences[mot] = frequences.GetValueOrDefault(mot) + 1;
}

System.Console.WriteLine(frequences.GetValueOrDefault(cible));
