using System.Collections.Generic;
using Domain;

namespace Infrastructure;

// Couche INFRASTRUCTURE : l'adaptateur concret du port. Peut dépendre de Domain (vers l'intérieur).
public sealed class DepotMemoire : IDepotTaches
{
    // TODO : stocker les tâches en mémoire (List<Tache>) et un compteur d'id auto-incrémenté à partir de 1.
    // TODO : implémenter Ajouter / Trouver / Lister.
}
