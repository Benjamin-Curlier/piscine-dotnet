using System;
using System.Linq;
using System.Collections.Generic;

var mots = new List<string>();
string ligne;
while ((ligne = System.Console.ReadLine()) is not null && ligne.Length > 0)
{
    mots.Add(ligne.Trim());
}

var top = mots
    .GroupBy(mot => mot)
    .OrderByDescending(groupe => groupe.Count())
    .ThenBy(groupe => groupe.Key, StringComparer.Ordinal)
    .First();

System.Console.WriteLine($"{top.Key} {top.Count()}");
