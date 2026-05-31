using System;

// Une "union" : Forme ne peut être QUE Cercle, Rectangle ou Carre (hiérarchie scellée).
var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

Forme forme = ligne[0] switch
{
    "cercle" => new Cercle(int.Parse(ligne[1])),
    "rectangle" => new Rectangle(int.Parse(ligne[1]), int.Parse(ligne[2])),
    "carre" => new Carre(int.Parse(ligne[1])),
    _ => throw new ArgumentException("forme inconnue")
};

// Le switch sur le TYPE est exhaustif : chaque variante a sa branche.
var aire = forme switch
{
    Cercle c => 3 * c.Rayon * c.Rayon,
    Rectangle r => r.Largeur * r.Hauteur,
    Carre ca => ca.Cote * ca.Cote,
    _ => 0
};

System.Console.WriteLine(aire);

abstract record Forme;
sealed record Cercle(int Rayon) : Forme;
sealed record Rectangle(int Largeur, int Hauteur) : Forme;
sealed record Carre(int Cote) : Forme;
