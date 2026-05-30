using System.Linq;

var nombres = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

System.Console.WriteLine($"Somme: {nombres.Sum()}");
System.Console.WriteLine($"Min: {nombres.Min()}");
System.Console.WriteLine($"Max: {nombres.Max()}");
System.Console.WriteLine($"Moyenne: {(int)nombres.Average()}");
