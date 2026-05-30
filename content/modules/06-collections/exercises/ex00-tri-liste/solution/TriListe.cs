var morceaux = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var nombres = new System.Collections.Generic.List<int>();
foreach (var morceau in morceaux)
{
    nombres.Add(int.Parse(morceau));
}
nombres.Sort();
System.Console.WriteLine(string.Join(' ', nombres));
