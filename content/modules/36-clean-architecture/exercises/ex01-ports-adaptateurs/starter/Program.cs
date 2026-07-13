using Application;
using Infrastructure;

// Composition root : câble les DEUX implémentations concrètes, puis lis les commandes et écris la sortie.
// TODO : var bibliotheque = new Bibliotheque(new DepotMemoire(), new NotificateurConsole());
// TODO : lire un entier N, puis N lignes "commande [argument]" :
//   - "add <titre>"     -> "Ajouté : #<id> <titre>"
//   - "emprunt <titre>" -> "Emprunt OK : <titre>" si emprunté, sinon "Emprunt refusé : <titre>"
//   - "rendre <titre>"  -> "Retour OK : <titre>" si rendu, sinon "Retour refusé : <titre>"
//   - "list"            -> une ligne par livre : "#<id> <titre> [emprunté]" ou "#<id> <titre> [disponible]"
// (les notifications "[notif] ..." sont émises par le NotificateurConsole au moment de l'emprunt/retour)
