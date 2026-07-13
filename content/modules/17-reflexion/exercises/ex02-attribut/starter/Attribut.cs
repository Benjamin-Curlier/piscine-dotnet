// Définis un attribut EtiquetteAttribute (une propriété Texte), applique-le sur plusieurs classes
// avec un texte DIFFÉRENT chacune ([Etiquette("Coucou")] sur MaClasse, etc.).
// Lis un nom de classe sur l'entrée standard ("MaClasse", "Produit" ou "Client"), sélectionne le
// type (switch => typeof(...)), puis affiche le texte de son attribut.
// Astuce : type.GetCustomAttribute<EtiquetteAttribute>()!.Texte
