using System;
using System.Collections.Generic;
using System.Linq;

var count = int.Parse(Console.ReadLine()!);
var traces = new Dictionary<string, (int Duration, int Errors)>(StringComparer.Ordinal);

for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ');
    var traceId = parts[0];
    var duration = int.Parse(parts[2]);
    var current = traces.GetValueOrDefault(traceId);
    traces[traceId] = (current.Duration + duration, current.Errors + (parts[3] == "ERROR" ? 1 : 0));
}

foreach (var trace in traces.OrderBy(pair => pair.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"{trace.Key} DUREE {trace.Value.Duration} ERREURS {trace.Value.Errors}");
}
