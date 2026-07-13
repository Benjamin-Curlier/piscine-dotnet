using System;
using Microsoft.Extensions.DependencyInjection;

// Le Main et les deux compteurs te sont fournis. À toi de renseigner Conteneur.Construire() :
// enregistre CompteurSingleton et CompteurTransient avec la BONNE durée de vie, puis renvoie le
// fournisseur construit. Le singleton doit garder son état (1 puis 2), le transient repartir de
// zéro (1 puis 1).

var provider = Conteneur.Construire();

Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());
Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());

static class Conteneur
{
    public static IServiceProvider Construire()
    {
        var services = new ServiceCollection();
        // TODO : enregistre CompteurSingleton en singleton et CompteurTransient en transient.
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
