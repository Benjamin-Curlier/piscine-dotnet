using System.Collections.Generic;
using Domain;

namespace Application;

// Cas d'usage : orchestre le domaine via le PORT (IDepotTaches), jamais via une implémentation concrète.
public sealed class GestionTaches
{
    private readonly IDepotTaches _depot;

    public GestionTaches(IDepotTaches depot) => _depot = depot;

    public Tache Ajouter(string titre) => _depot.Ajouter(titre);

    public bool Terminer(int id)
    {
        var tache = _depot.Trouver(id);
        if (tache is null)
        {
            return false;
        }

        tache.Terminer();
        return true;
    }

    public IReadOnlyList<Tache> Lister() => _depot.Lister();
}
