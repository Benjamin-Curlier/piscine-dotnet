using System.Linq;
using Application;
using Domain;
using Infrastructure;

// Composition root : le SEUL endroit qui connaît les implémentations concrètes et les câble.
var gestion = new GestionTaches(new DepotMemoire());

var n = int.Parse(System.Console.ReadLine()!);
for (var i = 0; i < n; i++)
{
    var ligne = System.Console.ReadLine()!;
    var espace = ligne.IndexOf(' ');
    var commande = espace < 0 ? ligne : ligne[..espace];
    var argument = espace < 0 ? string.Empty : ligne[(espace + 1)..];

    switch (commande)
    {
        case "add":
            var ajoutee = gestion.Ajouter(argument);
            System.Console.WriteLine($"Ajoutée : #{ajoutee.Id} {ajoutee.Titre}");
            break;
        case "done":
            var ok = gestion.Terminer(int.Parse(argument));
            System.Console.WriteLine(ok ? $"Faite : #{argument}" : $"Inconnue : #{argument}");
            break;
        case "list":
            foreach (var tache in gestion.Lister())
            {
                var marque = tache.Faite ? "x" : " ";
                System.Console.WriteLine($"#{tache.Id} [{marque}] {tache.Titre}");
            }

            break;
    }
}

var taches = gestion.Lister();
var faites = taches.Count(t => t.Faite);
System.Console.WriteLine($"Résumé : {taches.Count} tâche(s), {faites} faite(s)");
