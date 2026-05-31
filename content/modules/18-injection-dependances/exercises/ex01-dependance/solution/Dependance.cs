using Microsoft.Extensions.DependencyInjection;

var n = int.Parse(System.Console.ReadLine());

var services = new ServiceCollection();
services.AddSingleton<Multiplieur>();
services.AddSingleton<Traitement>();
var provider = services.BuildServiceProvider();

var traitement = provider.GetRequiredService<Traitement>();
System.Console.WriteLine(traitement.Traiter(n));

class Multiplieur
{
    public int Doubler(int x) => x * 2;
}

class Traitement
{
    private readonly Multiplieur _m;
    public Traitement(Multiplieur m) => _m = m;
    public int Traiter(int n) => _m.Doubler(n);
}
