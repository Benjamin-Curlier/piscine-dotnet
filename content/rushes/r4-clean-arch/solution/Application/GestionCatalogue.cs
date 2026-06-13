using System.Collections.Generic;
using Domain;

namespace Application;

// Cas d'usage : orchestre le domaine via le PORT (ICatalogueProduits), jamais via une implémentation concrète.
public sealed class GestionCatalogue
{
    private readonly ICatalogueProduits _catalogue;

    public GestionCatalogue(ICatalogueProduits catalogue) => _catalogue = catalogue;

    public Produit Ajouter(string nom, decimal prix) => _catalogue.Ajouter(nom, prix);

    public bool MettreAJourPrix(int id, decimal nouveauPrix)
    {
        var produit = _catalogue.Trouver(id);
        if (produit is null)
        {
            return false;
        }

        produit.MettreAJourPrix(nouveauPrix);
        return true;
    }

    public IReadOnlyList<Produit> Lister() => _catalogue.Lister();

    public Produit? Trouver(int id) => _catalogue.Trouver(id);
}
