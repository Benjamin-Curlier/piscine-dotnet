var tokens = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// On part d'un café, puis on l'emballe successivement dans des décorateurs.
IBoisson boisson = new Cafe();
foreach (var t in tokens)
{
    boisson = t switch
    {
        "lait" => new Lait(boisson),
        "sucre" => new Sucre(boisson),
        "chocolat" => new Chocolat(boisson),
        _ => boisson
    };
}

System.Console.WriteLine(boisson.Description() + " : " + boisson.Cout());

interface IBoisson
{
    string Description();
    int Cout();
}

sealed class Cafe : IBoisson
{
    public string Description() => "Café";
    public int Cout() => 2;
}

abstract class Decorateur : IBoisson
{
    protected readonly IBoisson Enveloppe;

    protected Decorateur(IBoisson enveloppe) => Enveloppe = enveloppe;

    public abstract string Description();
    public abstract int Cout();
}

sealed class Lait : Decorateur
{
    public Lait(IBoisson b) : base(b) { }
    public override string Description() => Enveloppe.Description() + ", lait";
    public override int Cout() => Enveloppe.Cout() + 1;
}

sealed class Sucre : Decorateur
{
    public Sucre(IBoisson b) : base(b) { }
    public override string Description() => Enveloppe.Description() + ", sucre";
    public override int Cout() => Enveloppe.Cout() + 1;
}

sealed class Chocolat : Decorateur
{
    public Chocolat(IBoisson b) : base(b) { }
    public override string Description() => Enveloppe.Description() + ", chocolat";
    public override int Cout() => Enveloppe.Cout() + 2;
}
