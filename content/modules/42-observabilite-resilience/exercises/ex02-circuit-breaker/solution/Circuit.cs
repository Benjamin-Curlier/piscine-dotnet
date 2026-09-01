using System;

var settings = Console.ReadLine()!.Split(' ');
var threshold = int.Parse(settings[0]);
var cooldown = int.Parse(settings[1]);
var count = int.Parse(Console.ReadLine()!);

var state = CircuitState.Closed;
var consecutiveFailures = 0;
var skipsRemaining = 0;

for (var index = 0; index < count; index++)
{
    var outcome = Console.ReadLine()!;
    if (state == CircuitState.Open && skipsRemaining > 0)
    {
        skipsRemaining--;
        Console.WriteLine("SKIP");
        continue;
    }

    if (state == CircuitState.Open)
    {
        if (outcome == "OK")
        {
            state = CircuitState.Closed;
            consecutiveFailures = 0;
            Console.WriteLine("PROBE_OK");
        }
        else
        {
            skipsRemaining = cooldown;
            Console.WriteLine("PROBE_FAIL");
        }

        continue;
    }

    if (outcome == "OK")
    {
        consecutiveFailures = 0;
        Console.WriteLine("OK");
        continue;
    }

    consecutiveFailures++;
    if (consecutiveFailures >= threshold)
    {
        state = CircuitState.Open;
        skipsRemaining = cooldown;
        Console.WriteLine("OPEN");
    }
    else
    {
        Console.WriteLine("FAIL");
    }
}

Console.WriteLine($"STATE {state.ToString().ToUpperInvariant()}");

enum CircuitState { Closed, Open }
