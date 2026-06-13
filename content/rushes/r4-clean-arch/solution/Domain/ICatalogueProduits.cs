using System.Collections.Generic;

namespace Domain;

// Port (interface) défini par le domaine : contrat de persistance, sans détail technique.
public interface ICatalogueProduits
{
    Produit Ajouter(string nom, decimal prix);

    Produit? Trouver(int id);

    IReadOnlyList<Produit> Lister();
}
