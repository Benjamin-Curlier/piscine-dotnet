using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Infrastructure;

// Adaptateur : implémente le port du domaine. Peut dépendre de Domain (vers l'intérieur), jamais l'inverse.
public sealed class DepotMemoire : IDepotTaches
{
    private readonly List<Tache> _taches = new();
    private int _prochainId = 1;

    public Tache Ajouter(string titre)
    {
        var tache = new Tache(_prochainId, titre);
        _prochainId++;
        _taches.Add(tache);
        return tache;
    }

    public Tache? Trouver(int id) => _taches.FirstOrDefault(t => t.Id == id);

    public IReadOnlyList<Tache> Lister() => _taches;
}
