using System.Linq;
using System.Reflection;

// Lis le nom de la classe à inspecter sur stdin ; sélectionne le type correspondant.
var cible = System.Console.ReadLine();

System.Type type = cible switch
{
    "Client" => typeof(Client),
    "Commande" => typeof(Commande),
    _ => typeof(Produit),
};

// Réflexion : liste les noms de propriétés du type choisi, triés pour un résultat déterministe.
var noms = type.GetProperties().Select(p => p.Name).OrderBy(n => n);
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

class Client
{
    public string Adresse { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
}

class Commande
{
    public System.DateTime Date { get; set; }
    public int Numero { get; set; }
    public double Total { get; set; }
}
