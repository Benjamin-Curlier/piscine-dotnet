using System.Collections.Generic;

namespace Domain;

// Port (interface) défini par le domaine : contrat de persistance, sans détail technique.
// TODO : Ajouter(nom, prix) -> Produit, Trouver(id) -> Produit?, Lister() -> IReadOnlyList<Produit>.
public interface ICatalogueProduits
{
}
