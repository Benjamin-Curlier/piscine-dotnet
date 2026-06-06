using System.Collections.Generic;

namespace Domain;

// Couche DOMAIN : le PORT de persistance. Le domaine définit le contrat ; l'infrastructure l'implémente.
public interface IDepotTaches
{
    // TODO : Ajouter(string titre) -> Tache ; Trouver(int id) -> Tache? ; Lister() -> IReadOnlyList<Tache>.
}
