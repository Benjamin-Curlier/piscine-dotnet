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
    // TODO : compare les segments et traite * puis >.
    return false;
}
