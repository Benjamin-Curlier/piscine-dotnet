using System;
using System.Collections.Generic;
using System.Linq;

var count = int.Parse(Console.ReadLine()!);
var dependencies = new Dictionary<string, string>(StringComparer.Ordinal);
for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    dependencies[parts[0]] = parts[1];
}

var started = new HashSet<string>(StringComparer.Ordinal);
var order = new List<string>();

while (order.Count < dependencies.Count)
{
    var next = dependencies
        .Where(pair => !started.Contains(pair.Key) && (pair.Value == "-" || started.Contains(pair.Value)))
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => pair.Key)
        .FirstOrDefault();

    if (next is null)
    {
        Console.WriteLine("CYCLE");
        return;
    }

    started.Add(next);
    order.Add(next);
}

foreach (var resource in order)
{
    Console.WriteLine(resource);
}
