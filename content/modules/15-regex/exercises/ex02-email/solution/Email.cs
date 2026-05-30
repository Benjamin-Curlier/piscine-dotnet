using System.Text.RegularExpressions;

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var ligne = System.Console.ReadLine();
    System.Console.WriteLine(Regex.IsMatch(ligne, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ? "valide" : "invalide");
}
