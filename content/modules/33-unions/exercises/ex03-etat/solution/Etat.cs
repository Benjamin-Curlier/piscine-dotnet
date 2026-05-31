using System;

// Machine à états comme union : l'état porte SES données (pas de champs inutilisés).
var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

Etat etat = ligne[0] switch
{
    "attente" => new EnAttente(),
    "cours" => new EnCours(int.Parse(ligne[1])),
    "termine" => new Termine(ligne[1]),
    _ => throw new ArgumentException("etat inconnu")
};

var description = etat switch
{
    EnAttente => "en attente",
    EnCours e => "en cours a " + e.Pourcent + "%",
    Termine t => "termine: " + t.Resultat,
    _ => ""
};

System.Console.WriteLine(description);

abstract record Etat;
sealed record EnAttente : Etat;
sealed record EnCours(int Pourcent) : Etat;
sealed record Termine(string Resultat) : Etat;
