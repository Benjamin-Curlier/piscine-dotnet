using System.Globalization;
using Application;
using Domain;
using Infrastructure;

// Composition root : le SEUL endroit qui connaît les implémentations concrètes et les câble.
var gestion = new GestionCatalogue(new CatalogueMemoire());

var n = int.Parse(System.Console.ReadLine()!);
for (var i = 0; i < n; i++)
{
    var ligne = System.Console.ReadLine()!;
    var parties = ligne.Split(' ');
    var commande = parties[0];

    switch (commande)
    {
        case "add":
            var nom = parties[1];
            var prix = decimal.Parse(parties[2], CultureInfo.InvariantCulture);
            var ajoute = gestion.Ajouter(nom, prix);
            System.Console.WriteLine($"Ajouté : #{ajoute.Id} {ajoute.Nom} ({ajoute.Prix:F2})");
            break;
        case "price":
            var idPrice = int.Parse(parties[1]);
            var nvPrix = decimal.Parse(parties[2], CultureInfo.InvariantCulture);
            var okPrice = gestion.MettreAJourPrix(idPrice, nvPrix);
            System.Console.WriteLine(okPrice ? $"Mis à jour : #{idPrice}" : $"Inconnu : #{idPrice}");
            break;
        case "get":
            var idGet = int.Parse(parties[1]);
            var produit = gestion.Trouver(idGet);
            if (produit is null)
            {
                System.Console.WriteLine($"Inconnu : #{idGet}");
            }
            else
            {
                System.Console.WriteLine($"#{produit.Id} {produit.Nom} ({produit.Prix:F2})");
            }

            break;
        case "list":
            foreach (var p in gestion.Lister())
            {
                System.Console.WriteLine($"#{p.Id} {p.Nom} ({p.Prix:F2})");
            }

            break;
    }
}

var tous = gestion.Lister();
System.Console.WriteLine($"Catalogue : {tous.Count} produit(s)");
