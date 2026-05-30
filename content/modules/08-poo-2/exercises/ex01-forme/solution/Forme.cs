using System.Collections.Generic;

var cote = int.Parse(System.Console.ReadLine());
var largeur = int.Parse(System.Console.ReadLine());
var hauteur = int.Parse(System.Console.ReadLine());

var formes = new List<IForme>
{
    new Carre { Cote = cote },
    new Rectangle { Largeur = largeur, Hauteur = hauteur },
};

foreach (var forme in formes)
{
    System.Console.WriteLine(forme.Aire());
}

interface IForme
{
    int Aire();
}

class Carre : IForme
{
    public int Cote { get; set; }
    public int Aire() => Cote * Cote;
}

class Rectangle : IForme
{
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public int Aire() => Largeur * Hauteur;
}
