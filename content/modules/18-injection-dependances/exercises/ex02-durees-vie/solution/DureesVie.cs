using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<CompteurSingleton>();
services.AddTransient<CompteurTransient>();
var provider = services.BuildServiceProvider();

System.Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
System.Console.WriteLine("Singleton: " + provider.GetRequiredService<CompteurSingleton>().Incrementer());
System.Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());
System.Console.WriteLine("Transient: " + provider.GetRequiredService<CompteurTransient>().Incrementer());

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
