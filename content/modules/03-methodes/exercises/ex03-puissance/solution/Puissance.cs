var b = long.Parse(System.Console.ReadLine());
var e = int.Parse(System.Console.ReadLine());

System.Console.WriteLine(Pow(b, e));

static long Pow(long b, int e) => e == 0 ? 1 : b * Pow(b, e - 1);
