using System.Collections.Generic;
using System.Linq;

var ligne = System.Console.ReadLine();
var k = int.Parse(System.Console.ReadLine());

var mots = ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var freq = new Dictionary<string, int>();
foreach (var mot in mots)
    freq[mot] = freq.GetValueOrDefault(mot) + 1;

var top = freq
    .OrderByDescending(p => p.Value)
    .ThenBy(p => p.Key)
    .Take(k);

foreach (var p in top)
    System.Console.WriteLine(p.Key);
