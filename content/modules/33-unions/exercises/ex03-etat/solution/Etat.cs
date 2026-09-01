using System;

var n = int.Parse(System.Console.ReadLine());
Etat etat = new EnAttente();

for (var i = 0; i < n; i++)
{
    var commande = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    var nouvelEtat = Transition(etat, commande);
    if (nouvelEtat is null)
    {
        System.Console.WriteLine("transition refusee");
        continue;
    }

    etat = nouvelEtat;
    System.Console.WriteLine(Decrire(etat));
}

static Etat? Transition(Etat etat, string[] commande) => (etat, commande[0]) switch
{
    (EnAttente, "demarrer") => new EnCours(0),
    (EnCours, "progres") when commande.Length == 2
        && int.TryParse(commande[1], out var p) && p >= 0 && p <= 100 => new EnCours(p),
    (EnCours, "terminer") when commande.Length == 2 => new Termine(commande[1]),
    _ => null,
};

static string Decrire(Etat etat) => etat switch
{
    EnCours e => "en cours a " + e.Pourcent + "%",
    Termine t => "termine: " + t.Resultat,
    EnAttente => "en attente",
    _ => throw new ArgumentOutOfRangeException(nameof(etat)),
};

abstract record Etat;
sealed record EnAttente : Etat;
sealed record EnCours(int Pourcent) : Etat;
sealed record Termine(string Resultat) : Etat;
