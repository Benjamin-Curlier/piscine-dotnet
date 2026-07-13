using Application;
using Infrastructure;

// Composition root : le SEUL endroit qui connaît les implémentations concrètes des deux ports et les câble.
var bibliotheque = new Bibliotheque(new DepotMemoire(), new NotificateurConsole());

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
            var livre = bibliotheque.Ajouter(argument);
            System.Console.WriteLine($"Ajouté : #{livre.Id} {livre.Titre}");
            break;
        case "emprunt":
            var emprunte = bibliotheque.Emprunter(argument);
            System.Console.WriteLine(emprunte ? $"Emprunt OK : {argument}" : $"Emprunt refusé : {argument}");
            break;
        case "rendre":
            var rendu = bibliotheque.Rendre(argument);
            System.Console.WriteLine(rendu ? $"Retour OK : {argument}" : $"Retour refusé : {argument}");
            break;
        case "list":
            foreach (var l in bibliotheque.Lister())
            {
                var etat = l.Emprunte ? "emprunté" : "disponible";
                System.Console.WriteLine($"#{l.Id} {l.Titre} [{etat}]");
            }

            break;
    }
}
