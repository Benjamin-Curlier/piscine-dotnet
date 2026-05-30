var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());
var c = int.Parse(System.Console.ReadLine());
System.Console.WriteLine(Max(a, Max(b, c)));

static int Max(int x, int y) => x > y ? x : y;
