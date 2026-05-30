using System;

var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());

try
{
    System.Console.WriteLine(a / b);
}
catch (DivideByZeroException)
{
    System.Console.WriteLine("Erreur: division par zero");
}
