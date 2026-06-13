using System.Collections.Generic;
using Domain;

namespace Application;

// Couche APPLICATION : le cas d'usage. Dépend de Domain (le port ICatalogueProduits), JAMAIS d'Infrastructure.
// TODO : recevoir un ICatalogueProduits par le constructeur (injection de dépendance).
// TODO : Ajouter(nom, prix), MettreAJourPrix(id, nouveauPrix) -> bool, Lister() -> IReadOnlyList<Produit>, Trouver(id) -> Produit?.
public sealed class GestionCatalogue
{
}
