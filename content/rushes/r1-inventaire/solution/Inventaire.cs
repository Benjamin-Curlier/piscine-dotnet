using System;
using System.Collections.Generic;
using System.Linq;

var inventory = new Inventory();
var commandCount = int.Parse(Console.ReadLine()!);

for (var index = 0; index < commandCount; index++)
{
    var parts = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    switch (parts[0])
    {
        case "ajouter":
            inventory.Add(parts[1], int.Parse(parts[2]));
            break;
        case "retirer":
            inventory.Remove(parts[1], int.Parse(parts[2]));
            break;
        case "afficher":
            Console.WriteLine($"{parts[1]}: {inventory.Quantity(parts[1])}");
            break;
        case "total":
            Console.WriteLine($"Total: {inventory.Total()}");
            break;
    }
}

public sealed class Inventory
{
    private readonly Dictionary<string, int> _stock = new(StringComparer.Ordinal);

    public void Add(string name, int quantity)
        => _stock[name] = Quantity(name) + quantity;

    public void Remove(string name, int quantity)
        => _stock[name] = Math.Max(0, Quantity(name) - quantity);

    public int Quantity(string name)
        => _stock.GetValueOrDefault(name);

    public int Total()
        => _stock.Values.Sum();
}
