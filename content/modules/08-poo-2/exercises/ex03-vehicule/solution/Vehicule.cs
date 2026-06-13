using System.Collections.Generic;

var km = int.Parse(System.Console.ReadLine());

var flotte = new List<Vehicule>
{
    new Voiture(),
    new Camion(),
};

foreach (var v in flotte)
    System.Console.WriteLine(v.LitresPour(km));

abstract class Vehicule
{
    protected abstract int LitresPour100Km { get; }
    public int LitresPour(int km) => km * LitresPour100Km / 100;
}

class Voiture : Vehicule
{
    protected override int LitresPour100Km => 7;
}

class Camion : Vehicule
{
    protected override int LitresPour100Km => 25;
}
