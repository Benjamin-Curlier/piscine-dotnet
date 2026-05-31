using Microsoft.Extensions.DependencyInjection;

var nom = System.Console.ReadLine();

var services = new ServiceCollection();
services.AddSingleton<IGreeter, Greeter>();
var provider = services.BuildServiceProvider();

var greeter = provider.GetRequiredService<IGreeter>();
System.Console.WriteLine(greeter.Saluer(nom));

interface IGreeter
{
    string Saluer(string nom);
}

class Greeter : IGreeter
{
    public string Saluer(string nom) => $"Bonjour, {nom}!";
}
