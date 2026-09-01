using System;

var workers = int.Parse(Console.ReadLine()!);
var count = int.Parse(Console.ReadLine()!);

for (var index = 0; index < count; index++)
{
    var message = Console.ReadLine()!;
    // TODO : calcule un numéro entre 1 et workers.
    Console.WriteLine($"worker-? {message}");
}
