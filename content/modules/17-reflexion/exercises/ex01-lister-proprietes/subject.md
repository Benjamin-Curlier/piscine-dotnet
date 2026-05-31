# ex01-lister-proprietes — Lister les propriétés

## Objectif

Affiche le **nom de toutes les propriétés** de la classe `Produit`, **triés par ordre
alphabétique** (pour un résultat déterministe), un par ligne.

Il n'y a **rien à lire** sur l'entrée.

Exemple de sortie : `Nom`, puis `Prix`, puis `Quantite`.

## Livrable

- `ListerProprietes.cs`

## Indices

- `typeof(Produit).GetProperties()` renvoie un tableau de `PropertyInfo`.
- Chaque `PropertyInfo` a une propriété `Name`.
- L'ordre renvoyé par la réflexion n'est pas garanti : trie avec `OrderBy(n => n)` pour un
  résultat stable.
- `Select` et `OrderBy` exigent `using System.Linq;` ; la réflexion exige
  `using System.Reflection;`.
