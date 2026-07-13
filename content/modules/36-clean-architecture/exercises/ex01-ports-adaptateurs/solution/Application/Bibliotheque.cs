using System.Collections.Generic;
using Domain;

namespace Application;

// Cas d'usage : orchestre le domaine via ses DEUX ports (IDepotLivres, INotificateur), jamais via
// une implémentation concrète.
public sealed class Bibliotheque
{
    private readonly IDepotLivres _depot;
    private readonly INotificateur _notificateur;

    public Bibliotheque(IDepotLivres depot, INotificateur notificateur)
    {
        _depot = depot;
        _notificateur = notificateur;
    }

    public Livre Ajouter(string titre) => _depot.Ajouter(titre);

    public bool Emprunter(string titre)
    {
        var livre = _depot.Trouver(titre);
        if (livre is null || livre.Emprunte)
        {
            return false;
        }

        livre.Emprunter();
        _notificateur.Notifier($"« {titre} » emprunté");
        return true;
    }

    public bool Rendre(string titre)
    {
        var livre = _depot.Trouver(titre);
        if (livre is null || !livre.Emprunte)
        {
            return false;
        }

        livre.Rendre();
        _notificateur.Notifier($"« {titre} » rendu");
        return true;
    }

    public IReadOnlyList<Livre> Lister() => _depot.Lister();
}
