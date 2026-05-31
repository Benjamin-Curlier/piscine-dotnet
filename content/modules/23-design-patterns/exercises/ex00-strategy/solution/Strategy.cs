var operation = System.Console.ReadLine();
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());

IOperation strategie;
if (operation == "add")
{
    strategie = new Addition();
}
else
{
    strategie = new Multiplication();
}

System.Console.WriteLine(strategie.Appliquer(a, b));

interface IOperation
{
    int Appliquer(int a, int b);
}

class Addition : IOperation
{
    public int Appliquer(int a, int b) => a + b;
}

class Multiplication : IOperation
{
    public int Appliquer(int a, int b) => a * b;
}
