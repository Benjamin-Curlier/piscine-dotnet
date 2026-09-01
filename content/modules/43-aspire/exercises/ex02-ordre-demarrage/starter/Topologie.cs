using System;
using System.Collections.Generic;

var count = int.Parse(Console.ReadLine()!);
var dependencies = new Dictionary<string, string>(StringComparer.Ordinal);
for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    dependencies[parts[0]] = parts[1];
}

// TODO : produis un ordre topologique déterministe ou CYCLE.
