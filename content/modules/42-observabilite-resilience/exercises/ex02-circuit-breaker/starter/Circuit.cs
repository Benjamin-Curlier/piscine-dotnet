using System;

var settings = Console.ReadLine()!.Split(' ');
var threshold = int.Parse(settings[0]);
var cooldown = int.Parse(settings[1]);
var count = int.Parse(Console.ReadLine()!);

var state = CircuitState.Closed;

for (var index = 0; index < count; index++)
{
    var outcome = Console.ReadLine()!;
    // TODO : fais évoluer le circuit et affiche la décision de cet événement.
    Console.WriteLine(outcome);
}

Console.WriteLine($"STATE {state.ToString().ToUpperInvariant()}");

enum CircuitState { Closed, Open }
