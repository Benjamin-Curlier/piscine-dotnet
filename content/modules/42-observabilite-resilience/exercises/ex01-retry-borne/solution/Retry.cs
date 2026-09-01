using System;

var maxAttempts = int.Parse(Console.ReadLine()!);
var outcomes = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
var attempts = 0;

for (var index = 0; index < outcomes.Length && attempts < maxAttempts; index++)
{
    attempts++;
    if (outcomes[index] == "OK")
    {
        Console.WriteLine($"SUCCESS {attempts}");
        return;
    }

    if (outcomes[index] == "FATAL")
    {
        Console.WriteLine($"FAILURE {attempts}");
        return;
    }
}

Console.WriteLine($"FAILURE {attempts}");
