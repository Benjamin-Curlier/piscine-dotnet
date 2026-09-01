using System;
using System.Collections.Generic;

var count = int.Parse(Console.ReadLine()!);
var applied = new HashSet<int>();
var total = 0;

for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    var sequence = int.Parse(parts[0]);
    var value = int.Parse(parts[1]);
    var outcome = parts[2];

    if (outcome == "OK" && applied.Add(sequence))
    {
        total += value;
        Console.WriteLine($"ACK {sequence}");
    }
    else if (outcome == "OK")
    {
        Console.WriteLine($"ACK_DUPLICATE {sequence}");
    }
    else
    {
        Console.WriteLine($"NAK {sequence}");
    }
}

Console.WriteLine($"TOTAL {total}");
