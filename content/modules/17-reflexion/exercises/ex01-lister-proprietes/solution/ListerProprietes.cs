using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

foreach (var nom in Reflexion.ListerProprietes(typeof(Produit)))
{
    Console.WriteLine(nom);
}

static class Reflexion
{
    // Renvoie le nom de TOUTES les propriétés publiques du type, triés par ordre alphabétique.
    public static IEnumerable<string> ListerProprietes(Type type)
    {
        return type.GetProperties().Select(p => p.Name).OrderBy(n => n);
    }
}

class Produit
{
    public string Nom { get; set; } = string.Empty;
    public double Prix { get; set; }
    public int Quantite { get; set; }
}
