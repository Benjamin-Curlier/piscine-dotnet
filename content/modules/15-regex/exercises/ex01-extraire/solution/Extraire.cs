using System.Text.RegularExpressions;

var ligne = System.Console.ReadLine();
foreach (Match m in Regex.Matches(ligne, @"\d+"))
{
    System.Console.WriteLine(m.Value);
}
