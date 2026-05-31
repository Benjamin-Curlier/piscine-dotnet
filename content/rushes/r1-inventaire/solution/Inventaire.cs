using System.Collections.Generic;
using System.Linq;

var stock = new Dictionary<string, int>();

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var ligne = System.Console.ReadLine();
    var parts = ligne.Split(' ');
    var commande = parts[0];

    if (commande == "ajouter")
    {
        var nom = parts[1];
        var qte = int.Parse(parts[2]);
        if (stock.ContainsKey(nom))
        {
            stock[nom] += qte;
        }
        else
        {
            stock[nom] = qte;
        }
    }
    else if (commande == "retirer")
    {
        var nom = parts[1];
        var qte = int.Parse(parts[2]);
        if (stock.ContainsKey(nom))
        {
            var reste = stock[nom] - qte;
            stock[nom] = reste < 0 ? 0 : reste;
        }
    }
    else if (commande == "afficher")
    {
        var nom = parts[1];
        var qte = stock.ContainsKey(nom) ? stock[nom] : 0;
        System.Console.WriteLine(nom + ": " + qte);
    }
    else if (commande == "total")
    {
        var total = stock.Values.Sum();
        System.Console.WriteLine("Total: " + total);
    }
}
