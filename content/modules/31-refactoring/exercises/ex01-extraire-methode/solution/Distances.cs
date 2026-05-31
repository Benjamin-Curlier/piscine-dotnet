// Version refactorée : la logique dupliquée est extraite dans une méthode Distance.
var v = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var a = int.Parse(v[0]);
var b = int.Parse(v[1]);
var c = int.Parse(v[2]);
var d = int.Parse(v[3]);

System.Console.WriteLine(Distance(a, b) + Distance(c, d));

static int Distance(int x, int y) => System.Math.Abs(x - y);
