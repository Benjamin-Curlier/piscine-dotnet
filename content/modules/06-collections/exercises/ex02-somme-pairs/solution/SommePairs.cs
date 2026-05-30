using System.Linq;

var somme = System.Console.ReadLine()
    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
    .Select(int.Parse)
    .Where(x => x % 2 == 0)
    .Sum();

System.Console.WriteLine(somme);
