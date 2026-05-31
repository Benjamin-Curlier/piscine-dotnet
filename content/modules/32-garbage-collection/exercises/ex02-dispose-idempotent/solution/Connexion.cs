var nom = System.Console.ReadLine();

var c = new Connexion(nom);
c.Dispose();
c.Dispose(); // un second Dispose ne doit RIEN refaire (idempotence).

sealed class Connexion : System.IDisposable
{
    private readonly string _nom;
    private bool _ferme;

    public Connexion(string nom)
    {
        _nom = nom;
        System.Console.WriteLine("ouvre " + _nom);
    }

    public void Dispose()
    {
        if (_ferme) { return; }
        _ferme = true;
        System.Console.WriteLine("ferme " + _nom);
    }
}
