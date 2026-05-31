using System.Collections.Generic;

// Lis des noms séparés par des espaces. Crée un Groupe (IDisposable) qui détient une
// liste de Ressource. Place-le dans un using, ajoute une Ressource par nom, affiche
// "travail". À la fin du using, le Groupe libère ses ressources en ordre inverse.

var noms = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : using (var groupe = new Groupe()) { ... } ; puis les classes Ressource et Groupe.
