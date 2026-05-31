var a = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
var b = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

var x1 = int.Parse(a[0]);
var y1 = int.Parse(a[1]);
var x2 = int.Parse(b[0]);
var y2 = int.Parse(b[1]);

var distance = System.Math.Abs(x1 - x2) + System.Math.Abs(y1 - y2);
System.Console.WriteLine(distance);
