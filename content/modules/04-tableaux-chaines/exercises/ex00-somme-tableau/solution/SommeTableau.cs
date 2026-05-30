var ligne = System.Console.ReadLine();
var somme = 0;
foreach (var morceau in ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
{
    somme += int.Parse(morceau);
}
System.Console.WriteLine(somme);
