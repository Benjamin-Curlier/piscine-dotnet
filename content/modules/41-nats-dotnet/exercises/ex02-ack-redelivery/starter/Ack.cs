using System;

var count = int.Parse(Console.ReadLine()!);
var total = 0;

for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    var sequence = int.Parse(parts[0]);
    var value = int.Parse(parts[1]);
    var outcome = parts[2];

    // TODO : n'applique que le premier OK et décide ACK/NAK.
    total += value;
    Console.WriteLine($"ACK {sequence}");
}

Console.WriteLine($"TOTAL {total}");
