var nom = System.Console.ReadLine();

// Le bloc using garantit l'appel à Dispose() à la sortie, quoi qu'il arrive.
using (var f = new Fichier(nom))
{
    System.Console.WriteLine("utilise " + nom);
}

sealed class Fichier : System.IDisposable
{
    private readonly string _nom;

    public Fichier(string nom)
    {
        _nom = nom;
        System.Console.WriteLine("ouvre " + _nom);
    }

    public void Dispose() => System.Console.WriteLine("ferme " + _nom);
}
