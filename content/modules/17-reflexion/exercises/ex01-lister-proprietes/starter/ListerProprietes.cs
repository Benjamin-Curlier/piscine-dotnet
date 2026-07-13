// Lis le nom d'une classe sur l'entrée standard ("Produit", "Client" ou "Commande"),
// sélectionne le type correspondant, puis affiche le nom de toutes ses propriétés,
// triées par ordre alphabétique, un par ligne.
// Astuce : var type = cible switch { "Client" => typeof(Client), ... _ => typeof(Produit) };
//          type.GetProperties().Select(p => p.Name).OrderBy(n => n)
// Définis les classes Produit, Client et Commande (avec des propriétés différentes).
