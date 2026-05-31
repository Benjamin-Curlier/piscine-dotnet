using System.Collections.Generic;

var noms = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// Un objet "propriétaire" libère lui-même les ressources qu'il détient (en ordre inverse).
using (var groupe = new Groupe())
{
    foreach (var n in noms)
    {
        groupe.Ajouter(new Ressource(n));
    }
    System.Console.WriteLine("travail");
}

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

sealed class Groupe : System.IDisposable
{
    private readonly List<Ressource> _ressources = new();

    public void Ajouter(Ressource r) => _ressources.Add(r);

    public void Dispose()
    {
        for (var i = _ressources.Count - 1; i >= 0; i--)
        {
            _ressources[i].Dispose();
        }
    }
}
