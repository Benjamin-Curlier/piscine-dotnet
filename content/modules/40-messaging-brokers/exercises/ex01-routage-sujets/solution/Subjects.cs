using System;

var abonnement = Console.ReadLine()!;
var count = int.Parse(Console.ReadLine()!);

for (var index = 0; index < count; index++)
{
    var sujet = Console.ReadLine()!;
    if (Correspond(abonnement, sujet))
    {
        Console.WriteLine(sujet);
    }
}

static bool Correspond(string abonnement, string sujet)
{
    var motif = abonnement.Split('.');
    var segments = sujet.Split('.');

    for (var index = 0; index < motif.Length; index++)
    {
        if (motif[index] == ">")
        {
            return index < segments.Length;
        }

        if (index >= segments.Length || motif[index] != "*" && motif[index] != segments[index])
        {
            return false;
        }
    }

    return motif.Length == segments.Length;
}
