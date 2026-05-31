using System.Linq;
using System.Reflection;

var noms = typeof(Produit).GetProperties().Select(p => p.Name).OrderBy(n => n);
foreach (var nom in noms)
{
    System.Console.WriteLine(nom);
}

class Produit
{
    public string Nom { get; set; } = string.Empty;
    public double Prix { get; set; }
    public int Quantite { get; set; }
}
