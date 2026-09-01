using Application;
using Domain;
using Infrastructure;
using System;
using System.Text.Json;
using System.Threading;

var processor = new EventProcessor(new MemoryInbox(), new ConsoleAuditSink());
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var count = int.Parse(Console.ReadLine()!);

for (var index = 0; index < count; index++)
{
    var message = JsonSerializer.Deserialize<EventMessage>(Console.ReadLine()!, options)
        ?? throw new JsonException("Événement JSON vide.");
    await processor.ProcessAsync(message, CancellationToken.None);
}

Console.WriteLine($"FINAL {processor.Balance}");
