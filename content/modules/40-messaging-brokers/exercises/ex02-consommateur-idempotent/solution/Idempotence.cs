using System;
using System.Collections.Generic;

var count = int.Parse(Console.ReadLine()!);
var appliques = new HashSet<string>(StringComparer.Ordinal);
var total = 0;
var doublons = 0;

for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    var id = parts[0];
    var montant = int.Parse(parts[1]);
    if (appliques.Add(id))
    {
        total += montant;
    }
    else
    {
        doublons++;
    }
}

Console.WriteLine($"TOTAL {total}");
Console.WriteLine($"DOUBLONS {doublons}");
