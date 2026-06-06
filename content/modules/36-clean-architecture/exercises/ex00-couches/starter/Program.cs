using System.Linq;
using Application;
using Domain;
using Infrastructure;

// Composition root : câble les implémentations concrètes, puis lis les commandes et écris la sortie.
// TODO : var gestion = new GestionTaches(new DepotMemoire());
// TODO : lire un entier N, puis N lignes "commande [argument]" :
//   - "add <titre>"  -> "Ajoutée : #<id> <titre>"
//   - "done <id>"    -> "Faite : #<id>" si trouvée, sinon "Inconnue : #<id>"
//   - "list"         -> une ligne par tâche : "#<id> [x] <titre>" (faite) ou "#<id> [ ] <titre>"
// TODO : terminer par "Résumé : <total> tâche(s), <faites> faite(s)".
