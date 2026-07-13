using System.Collections.Generic;
using Domain;

namespace Application;

// Couche APPLICATION : le cas d'usage. Ne dépend QUE des ports (IDepotLivres, INotificateur),
// jamais des implémentations concrètes.
public sealed class Bibliotheque
{
    // TODO : reçois les deux ports par le constructeur (IDepotLivres, INotificateur).
    // TODO : Ajouter(titre) délègue au dépôt.
    // TODO : Emprunter(titre) -> false si introuvable ou déjà emprunté ; sinon emprunte,
    //        appelle Notifier("« <titre> » emprunté") et renvoie true.
    // TODO : Rendre(titre) -> false si introuvable ou pas emprunté ; sinon rend,
    //        appelle Notifier("« <titre> » rendu") et renvoie true.
    // TODO : Lister() renvoie la liste du dépôt.
}
