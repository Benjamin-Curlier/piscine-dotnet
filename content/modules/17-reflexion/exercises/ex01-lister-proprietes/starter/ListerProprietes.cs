using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Le Main (qui affiche le résultat pour la classe Produit) t'est fourni. À toi de compléter
// Reflexion.ListerProprietes(Type) : renvoie le nom de toutes les propriétés du type, triés par
// ordre alphabétique. La correction appelle ta méthode sur un AUTRE type que Produit : n'y code
// donc rien en dur, utilise bien la réflexion.

foreach (var nom in Reflexion.ListerProprietes(typeof(Produit)))
{
    Console.WriteLine(nom);
}

static class Reflexion
{
    public static IEnumerable<string> ListerProprietes(Type type)
    {
        // TODO : type.GetProperties().Select(p => p.Name).OrderBy(n => n)
        return new string[0];
    }
}

class Produit
{
    public string Nom { get; set; } = string.Empty;
    public double Prix { get; set; }
    public int Quantite { get; set; }
}
