using System;
using System.Reflection;

// Lis le nom de la classe à inspecter sur stdin ; sélectionne le type correspondant.
var cible = System.Console.ReadLine();

var type = cible switch
{
    "Produit" => typeof(Produit),
    "Client" => typeof(Client),
    _ => typeof(MaClasse),
};

// Réflexion : lis la valeur de l'attribut personnalisé porté par le type choisi.
var etiquette = type.GetCustomAttribute<EtiquetteAttribute>()!;
System.Console.WriteLine(etiquette.Texte);

class EtiquetteAttribute : Attribute
{
    public string Texte { get; }
    public EtiquetteAttribute(string texte) => Texte = texte;
}

[Etiquette("Coucou")]
class MaClasse { }

[Etiquette("Catalogue de produits")]
class Produit { }

[Etiquette("Fiche client")]
class Client { }
