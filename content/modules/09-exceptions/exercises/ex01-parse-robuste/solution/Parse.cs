using System;

var n = int.Parse(System.Console.ReadLine());

for (var i = 0; i < n; i++)
{
    var ligne = System.Console.ReadLine();
    try
    {
        var nombre = int.Parse(ligne);
        System.Console.WriteLine(nombre);
    }
    catch (FormatException)
    {
        System.Console.WriteLine("invalide");
    }
}
