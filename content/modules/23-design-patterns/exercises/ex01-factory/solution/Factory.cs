var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var type = System.Console.ReadLine();
    var animal = AnimalFactory.Creer(type);
    System.Console.WriteLine(animal.Cri());
}

interface IAnimal
{
    string Cri();
}

class Chien : IAnimal
{
    public string Cri() => "Wouf";
}

class Chat : IAnimal
{
    public string Cri() => "Miaou";
}

static class AnimalFactory
{
    public static IAnimal Creer(string type)
    {
        if (type == "chien")
        {
            return new Chien();
        }

        return new Chat();
    }
}
