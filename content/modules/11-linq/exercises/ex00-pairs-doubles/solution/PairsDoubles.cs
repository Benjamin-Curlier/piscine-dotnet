using System.Linq;

var nombres = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);

var resultat = nombres.Where(x => x % 2 == 0).Select(x => x * 2);

System.Console.WriteLine(string.Join(" ", resultat));
