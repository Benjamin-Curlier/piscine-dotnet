var noms = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// Les using declarations sont libérées en ordre INVERSE à la fin de la portée (LIFO).
using var a = new Ressource(noms[0]);
using var b = new Ressource(noms[1]);
using var c = new Ressource(noms[2]);

System.Console.WriteLine("travail");

sealed class Ressource : System.IDisposable
{
    private readonly string _nom;

    public Ressource(string nom)
    {
        _nom = nom;
        System.Console.WriteLine("ouvre " + _nom);
    }

    public void Dispose() => System.Console.WriteLine("ferme " + _nom);
}
