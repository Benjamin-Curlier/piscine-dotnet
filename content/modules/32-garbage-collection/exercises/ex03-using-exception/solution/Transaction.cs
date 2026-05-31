var nom = System.Console.ReadLine();

// Même si une exception est levée, le using libère la ressource pendant le déroulement
// de la pile, AVANT que le catch externe ne s'exécute.
try
{
    using var t = new Transaction(nom);
    System.Console.WriteLine("debut");
    throw new System.Exception("boom");
}
catch
{
    System.Console.WriteLine("erreur attrapee");
}

sealed class Transaction : System.IDisposable
{
    private readonly string _nom;

    public Transaction(string nom)
    {
        _nom = nom;
        System.Console.WriteLine("ouvre " + _nom);
    }

    public void Dispose() => System.Console.WriteLine("ferme " + _nom);
}
