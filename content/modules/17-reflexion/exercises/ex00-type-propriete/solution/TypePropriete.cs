using System.Reflection;

var nom = System.Console.ReadLine();
var propriete = typeof(Produit).GetProperty(nom)!;
System.Console.WriteLine(propriete.PropertyType.Name);

class Produit
{
    public string Nom { get; set; } = string.Empty;
    public double Prix { get; set; }
    public int Quantite { get; set; }
}
