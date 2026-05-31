using System;

var nom = System.Console.ReadLine();
var couleur = Enum.Parse<Couleur>(nom);
var hex = couleur switch
{
    Couleur.Rouge => "#FF0000",
    Couleur.Vert => "#00FF00",
    Couleur.Bleu => "#0000FF",
    _ => "#000000",
};
System.Console.WriteLine(hex);

enum Couleur
{
    Rouge,
    Vert,
    Bleu,
}
