using System;
using System.Linq;
using System.Reflection;

var personne = new Personne
{
    Nom = System.Console.ReadLine(),
    Age = int.Parse(System.Console.ReadLine()),
    Actif = bool.Parse(System.Console.ReadLine()),
};

foreach (PropertyInfo prop in personne.GetType()
             .GetProperties()
             .OrderBy(p => p.Name, StringComparer.Ordinal))
{
    System.Console.WriteLine($"{prop.Name}={prop.GetValue(personne)}");
}

sealed class Personne
{
    public string Nom { get; set; } = "";
    public int Age { get; set; }
    public bool Actif { get; set; }
}
