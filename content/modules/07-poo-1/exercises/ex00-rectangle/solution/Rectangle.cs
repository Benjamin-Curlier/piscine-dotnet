var largeur = int.Parse(System.Console.ReadLine());
var hauteur = int.Parse(System.Console.ReadLine());
var rect = new Rectangle { Largeur = largeur, Hauteur = hauteur };
System.Console.WriteLine(rect.Aire());

class Rectangle
{
    public int Largeur { get; set; }
    public int Hauteur { get; set; }
    public int Aire() => Largeur * Hauteur;
}
