using System;

var x = int.Parse(System.Console.ReadLine());
var y = int.Parse(System.Console.ReadLine());
var v = new Vecteur(x, y);
System.Console.WriteLine(v.Norme1);

readonly struct Vecteur(int x, int y)
{
    public int Norme1 => Math.Abs(x) + Math.Abs(y);
}
