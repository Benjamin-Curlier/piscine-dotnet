using System.Globalization;
using Application;
using Domain;
using Infrastructure;

// Composition root : câble les implémentations concrètes, puis lis les commandes et écris la sortie.
// TODO : var gestion = new GestionCatalogue(new CatalogueMemoire());
// TODO : lire un entier N, puis N lignes "commande [arguments]" :
//   - "add <nom> <prix>"   -> "Ajouté : #<id> <nom> (<prix:.2f>)"
//   - "price <id> <prix>"  -> "Mis à jour : #<id>" si trouvé, sinon "Inconnu : #<id>"
//   - "get <id>"           -> "#<id> <nom> (<prix:.2f>)" si trouvé, sinon "Inconnu : #<id>"
//   - "list"               -> une ligne "#<id> <nom> (<prix:.2f>)" par produit (ordre d'insertion)
// TODO : terminer par "Catalogue : <total> produit(s)".
