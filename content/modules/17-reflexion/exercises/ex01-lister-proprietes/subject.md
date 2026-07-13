# ex01-lister-proprietes — Lister les propriétés

## Objectif

Lis sur l'entrée standard le **nom d'une classe** (`Produit`, `Client` ou `Commande`),
sélectionne le **type** correspondant, puis affiche le **nom de toutes ses propriétés**, **triés
par ordre alphabétique** (pour un résultat déterministe), un par ligne.

Définis les trois classes (avec des propriétés **différentes**), par exemple :

- `Produit` → `Nom`, `Prix`, `Quantite` ;
- `Client` → `Adresse`, `Email`, `Nom` ;
- `Commande` → `Date`, `Numero`, `Total`.

Exemple : pour l'entrée `Client`, la sortie est `Adresse`, puis `Email`, puis `Nom`.

## Livrable

- `ListerProprietes.cs`

## Indices

- Lis le nom sur `System.Console.ReadLine()`, puis choisis le type avec un `switch` renvoyant
  `typeof(...)` (`"Client" => typeof(Client)`, etc.).
- `type.GetProperties()` renvoie un tableau de `PropertyInfo`.
- Chaque `PropertyInfo` a une propriété `Name`.
- L'ordre renvoyé par la réflexion n'est pas garanti : trie avec `OrderBy(n => n)` pour un
  résultat stable.
- `Select` et `OrderBy` exigent `using System.Linq;` ; la réflexion exige
  `using System.Reflection;`.
