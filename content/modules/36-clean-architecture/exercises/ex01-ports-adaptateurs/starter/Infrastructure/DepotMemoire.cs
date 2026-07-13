using System.Collections.Generic;
using Domain;

namespace Infrastructure;

// Couche INFRASTRUCTURE : l'adaptateur concret du port IDepotLivres. Peut dépendre de Domain.
public sealed class DepotMemoire : IDepotLivres
{
    // TODO : stocker les livres en mémoire (List<Livre>) + un compteur d'id auto-incrémenté à partir de 1.
    // TODO : implémenter Ajouter / Trouver / Lister.
}
