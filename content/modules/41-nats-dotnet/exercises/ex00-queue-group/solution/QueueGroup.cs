using System;

var workers = int.Parse(Console.ReadLine()!);
var count = int.Parse(Console.ReadLine()!);

for (var index = 0; index < count; index++)
{
    var message = Console.ReadLine()!;
    var worker = index % workers + 1;
    Console.WriteLine($"worker-{worker} {message}");
}
