using System.Linq;

var mots = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

var groupes = mots
    .GroupBy(m => m)
    .Select(g => new { Mot = g.Key, Compte = g.Count() })
    .OrderByDescending(x => x.Compte)
    .ThenBy(x => x.Mot);

foreach (var g in groupes)
    System.Console.WriteLine($"{g.Mot}: {g.Compte}");
