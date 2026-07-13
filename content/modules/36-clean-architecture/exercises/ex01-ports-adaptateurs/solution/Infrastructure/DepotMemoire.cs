using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Infrastructure;

// Adaptateur du port IDepotLivres : stocke les livres en mémoire. Dépend de Domain (vers l'intérieur).
public sealed class DepotMemoire : IDepotLivres
{
    private readonly List<Livre> _livres = new();
    private int _prochainId = 1;

    public Livre Ajouter(string titre)
    {
        var livre = new Livre(_prochainId, titre);
        _prochainId++;
        _livres.Add(livre);
        return livre;
    }

    public Livre? Trouver(string titre) => _livres.FirstOrDefault(l => l.Titre == titre);

    public IReadOnlyList<Livre> Lister() => _livres;
}
