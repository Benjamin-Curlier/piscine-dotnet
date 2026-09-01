using System;
using System.Collections.Generic;

var requestCount = int.Parse(Console.ReadLine()!);
var requests = new List<string>();
for (var index = 0; index < requestCount; index++)
{
    requests.Add(Console.ReadLine()!);
}

var replyCount = int.Parse(Console.ReadLine()!);
var replies = new Dictionary<string, string>(StringComparer.Ordinal);
for (var index = 0; index < replyCount; index++)
{
    var parts = Console.ReadLine()!.Split(' ', 2);
    replies[parts[0]] = parts[1];
}

foreach (var request in requests)
{
    Console.WriteLine(replies.TryGetValue(request, out var value)
        ? $"{request}={value}"
        : $"{request}=TIMEOUT");
}
