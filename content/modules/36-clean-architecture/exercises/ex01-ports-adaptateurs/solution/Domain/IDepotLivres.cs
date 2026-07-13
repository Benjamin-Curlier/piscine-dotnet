using System.Collections.Generic;

namespace Domain;

// Port de persistance : le contrat, défini par le domaine, sans détail technique.
public interface IDepotLivres
{
    Livre Ajouter(string titre);

    Livre? Trouver(string titre);

    IReadOnlyList<Livre> Lister();
}
