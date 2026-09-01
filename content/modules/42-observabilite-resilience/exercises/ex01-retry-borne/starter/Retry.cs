using System;

var maxAttempts = int.Parse(Console.ReadLine()!);
var outcomes = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

// TODO : arrête-toi sur OK, FATAL ou épuisement du budget.
Console.WriteLine($"FAILURE {maxAttempts}");
