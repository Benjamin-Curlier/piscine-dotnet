using System;

var nom = System.Console.ReadLine();
var c = Enum.Parse<Couleur>(nom);
System.Console.WriteLine((int)c);

enum Couleur
{
    Rouge,
    Vert,
    Bleu,
}
