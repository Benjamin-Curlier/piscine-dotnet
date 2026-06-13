using System.Collections.Generic;
using Domain;

namespace Infrastructure;

// Adaptateur : implémente le port du domaine. Peut dépendre de Domain (vers l'intérieur), jamais l'inverse.
public sealed class CatalogueMemoire : ICatalogueProduits
{
    private readonly List<Produit> _produits = new();
    private int _prochainId = 1;

    public Produit Ajouter(string nom, decimal prix)
    {
        var produit = new Produit(_prochainId, nom, prix);
        _prochainId++;
        _produits.Add(produit);
        return produit;
    }

    public Produit? Trouver(int id)
    {
        foreach (var p in _produits)
        {
            if (p.Id == id)
            {
                return p;
            }
        }

        return null;
    }

    public IReadOnlyList<Produit> Lister() => _produits;
}
