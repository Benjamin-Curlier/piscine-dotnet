using System;

// Type union "Resultat" : soit un Succes (valeur), soit une Erreur (message).
// Modélise l'échec sans exception ni valeur nulle.
var ligne = System.Console.ReadLine();
var parts = ligne.Split('/', System.StringSplitOptions.RemoveEmptyEntries);

Resultat r;
if (parts.Length == 2 && int.Parse(parts[1]) != 0)
{
    r = new Succes(int.Parse(parts[0]) / int.Parse(parts[1]));
}
else
{
    r = new Erreur("division par zero");
}

var message = r switch
{
    Succes s => "OK " + s.Valeur,
    Erreur e => "ERR " + e.Message,
    _ => ""
};

System.Console.WriteLine(message);

abstract record Resultat;
sealed record Succes(int Valeur) : Resultat;
sealed record Erreur(string Message) : Resultat;
