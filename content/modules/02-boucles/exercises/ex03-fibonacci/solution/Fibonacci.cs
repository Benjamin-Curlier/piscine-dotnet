var n = int.Parse(System.Console.ReadLine());

var a = 0;
var b = 1;
for (var i = 0; i < n; i++)
{
    var tmp = a + b;
    a = b;
    b = tmp;
}

System.Console.WriteLine(a);
