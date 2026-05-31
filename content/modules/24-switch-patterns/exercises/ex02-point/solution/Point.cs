var x = int.Parse(System.Console.ReadLine());
var y = int.Parse(System.Console.ReadLine());
var p = new Point(x, y);

var classe = p switch
{
    { X: 0, Y: 0 } => "origine",
    { X: 0 } => "axe Y",
    { Y: 0 } => "axe X",
    _ => "quelconque",
};

System.Console.WriteLine(classe);

record Point(int X, int Y);
