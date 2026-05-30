var n = int.Parse(System.Console.ReadLine());
System.Console.WriteLine(Factorielle(n));

static long Factorielle(int n) => n <= 1 ? 1 : n * Factorielle(n - 1);
