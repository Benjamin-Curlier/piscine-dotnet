var x = int.Parse(System.Console.ReadLine());
var y = int.Parse(System.Console.ReadLine());
var p = new Point(x, y);
var p2 = p with { X = p.X + 1 };
System.Console.WriteLine($"({p2.X}, {p2.Y})");

record Point(int X, int Y);
