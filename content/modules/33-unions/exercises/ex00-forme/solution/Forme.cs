using System;

// Une approximation de type somme : le programme prévoit trois variantes de Forme.
var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

Forme forme = ligne[0] switch
{
    "cercle" => new Cercle(int.Parse(ligne[1])),
    "rectangle" => new Rectangle(int.Parse(ligne[1]), int.Parse(ligne[2])),
    "carre" => new Carre(int.Parse(ligne[1])),
    _ => throw new ArgumentException("forme inconnue")
};

// C# 14 ne ferme pas la base Forme : le cas de repli signale toute variante inattendue.
var aire = forme switch
{
    Cercle c => 3 * c.Rayon * c.Rayon,
    Rectangle r => r.Largeur * r.Hauteur,
    Carre ca => ca.Cote * ca.Cote,
    _ => throw new ArgumentOutOfRangeException(nameof(forme))
};

System.Console.WriteLine(aire);

abstract record Forme;
sealed record Cercle(int Rayon) : Forme;
sealed record Rectangle(int Largeur, int Hauteur) : Forme;
sealed record Carre(int Cote) : Forme;
