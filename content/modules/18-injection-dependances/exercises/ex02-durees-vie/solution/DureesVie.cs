using Microsoft.Extensions.DependencyInjection;

// Lis N (le nombre de résolutions à effectuer pour chaque durée de vie).
var n = int.Parse(System.Console.ReadLine());

var services = new ServiceCollection();
services.AddSingleton<CompteurSingleton>();
services.AddTransient<CompteurTransient>();
var provider = services.BuildServiceProvider();

// Singleton : même instance à chaque résolution => l'état s'accumule (1, 2, 3, …).
for (var i = 0; i < n; i++)
{
    System.Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
}

// Transient : instance neuve à chaque résolution => l'état repart de zéro (1, 1, 1, …).
for (var i = 0; i < n; i++)
{
    System.Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());
}

class CompteurSingleton
{
    private int _n;
    public int Incrementer() => ++_n;
}

class CompteurTransient
{
    private int _n;
    public int Incrementer() => ++_n;
}
