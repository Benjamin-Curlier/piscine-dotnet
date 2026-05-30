using System.Collections.Generic;

var n = int.Parse(System.Console.ReadLine());
var animaux = new List<Animal>();
for (var i = 0; i < n; i++)
{
    var type = System.Console.ReadLine();
    if (type == "chien")
    {
        animaux.Add(new Chien());
    }
    else
    {
        animaux.Add(new Chat());
    }
}

foreach (var animal in animaux)
{
    System.Console.WriteLine(animal.Cri());
}

class Animal
{
    public virtual string Cri() => "...";
}

class Chien : Animal
{
    public override string Cri() => "Wouf";
}

class Chat : Animal
{
    public override string Cri() => "Miaou";
}
