using System.Collections.Generic;
using Domain;

namespace Infrastructure;

// Couche INFRASTRUCTURE : l'adaptateur. Implémente le port du domaine (ICatalogueProduits).
// Peut dépendre de Domain (vers l'intérieur), jamais l'inverse.
// TODO : stocker les produits en mémoire avec un compteur d'id ; implémenter Ajouter, Trouver, Lister.
public sealed class CatalogueMemoire : ICatalogueProduits
{
    // TODO : liste interne + compteur _prochainId.
    // TODO : Ajouter -> new Produit(_prochainId, nom, prix), incrémenter, ajouter à la liste.
    // TODO : Trouver -> chercher par Id (retourner null si absent).
    // TODO : Lister -> retourner la liste en lecture seule.
}
