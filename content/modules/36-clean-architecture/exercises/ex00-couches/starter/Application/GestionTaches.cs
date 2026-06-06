using System.Collections.Generic;
using Domain;

namespace Application;

// Couche APPLICATION : le cas d'usage. Dépend de Domain (le port IDepotTaches), JAMAIS d'Infrastructure.
public sealed class GestionTaches
{
    // TODO : recevoir un IDepotTaches par le constructeur (injection de dépendance).
    // TODO : Ajouter(titre), Terminer(id) -> bool, Lister() -> IReadOnlyList<Tache>.
}
