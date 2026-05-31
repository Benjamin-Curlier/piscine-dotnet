using System;

// Union récursive simplifiée : une valeur JSON est un nombre, une chaîne ou un booléen.
var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

Valeur v = ligne[0] switch
{
    "nombre" => new Nombre(int.Parse(ligne[1])),
    "texte" => new Texte(ligne[1]),
    "booleen" => new Booleen(ligne[1] == "vrai"),
    _ => throw new ArgumentException("type inconnu")
};

// Le rendu dépend de la variante (pattern matching avec déconstruction).
var rendu = v switch
{
    Nombre(var n) => n.ToString(),
    Texte(var t) => "\"" + t + "\"",
    Booleen(var b) => b ? "true" : "false",
    _ => ""
};

System.Console.WriteLine(rendu);

abstract record Valeur;
sealed record Nombre(int N) : Valeur;
sealed record Texte(string T) : Valeur;
sealed record Booleen(bool B) : Valeur;
