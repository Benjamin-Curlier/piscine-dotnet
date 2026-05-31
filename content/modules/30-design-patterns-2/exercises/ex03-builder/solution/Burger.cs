using System.Collections.Generic;

var extras = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// Construction fluide : chaque appel à Avec(...) renvoie le builder.
var builder = new BurgerBuilder();
foreach (var e in extras)
{
    builder.Avec(e);
}

System.Console.WriteLine(builder.Construire());

sealed class BurgerBuilder
{
    private readonly List<string> _ingredients = new() { "pain", "steak" };

    public BurgerBuilder Avec(string ingredient)
    {
        _ingredients.Add(ingredient);
        return this;
    }

    public string Construire() =>
        "Burger : " + string.Join(", ", _ingredients) + " (" + _ingredients.Count + " ingrédients)";
}
