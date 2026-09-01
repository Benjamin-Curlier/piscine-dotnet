using System;
using System.Collections.Generic;

var count = int.Parse(Console.ReadLine()!);
var total = 0;
var doublons = 0;

// TODO : conserve les identifiants déjà appliqués.
for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    var id = parts[0];
    var montant = int.Parse(parts[1]);
    total += montant;
}

Console.WriteLine($"TOTAL {total}");
Console.WriteLine($"DOUBLONS {doublons}");
