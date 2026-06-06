using System.Collections.Generic;

namespace Domain;

// Port (interface) défini par le domaine : le contrat de persistance, sans détail technique.
public interface IDepotTaches
{
    Tache Ajouter(string titre);

    Tache? Trouver(int id);

    IReadOnlyList<Tache> Lister();
}
