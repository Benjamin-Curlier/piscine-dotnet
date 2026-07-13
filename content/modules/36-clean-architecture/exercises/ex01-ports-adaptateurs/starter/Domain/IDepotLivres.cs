using System.Collections.Generic;

namespace Domain;

// Couche DOMAIN : le PORT de persistance. Le domaine définit le contrat ; l'infrastructure l'implémente.
public interface IDepotLivres
{
    // TODO : Ajouter(string titre) -> Livre ; Trouver(string titre) -> Livre? ; Lister() -> IReadOnlyList<Livre>.
}
