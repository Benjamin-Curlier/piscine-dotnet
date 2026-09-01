using Application;
using Domain;
using Infrastructure;
using System;
using System.Text.Json;

var processor = new EventProcessor(); // TODO : compose les adaptateurs.
var count = int.Parse(Console.ReadLine()!);
for (var index = 0; index < count; index++)
{
    // TODO : désérialise puis traite chaque message.
    _ = Console.ReadLine();
}

Console.WriteLine($"FINAL {processor.Balance}");
