using System;
using System.Collections.Generic;

var requestCount = int.Parse(Console.ReadLine()!);
var requests = new List<string>();
for (var index = 0; index < requestCount; index++)
{
    requests.Add(Console.ReadLine()!);
}

var replyCount = int.Parse(Console.ReadLine()!);
// TODO : indexe les réponses par correlationId.
for (var index = 0; index < replyCount; index++)
{
    _ = Console.ReadLine();
}

foreach (var request in requests)
{
    Console.WriteLine($"{request}=TIMEOUT");
}
