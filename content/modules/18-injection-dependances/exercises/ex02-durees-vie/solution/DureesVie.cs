using System;
using Microsoft.Extensions.DependencyInjection;

var provider = Conteneur.Construire();

Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());
Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());

static class Conteneur
{
    // Enregistre CompteurSingleton en SINGLETON (une seule instance partagée) et CompteurTransient
    // en TRANSIENT (une instance neuve à chaque résolution), puis construit le fournisseur.
    public static IServiceProvider Construire()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CompteurSingleton>();
        services.AddTransient<CompteurTransient>();
        return services.BuildServiceProvider();
    }
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
