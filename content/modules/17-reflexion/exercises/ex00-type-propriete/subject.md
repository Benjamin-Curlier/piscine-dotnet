# ex00-type-propriete — Le type d'une propriété

## Objectif

On dispose d'une classe `Produit` avec trois propriétés : `Nom`, `Prix` et `Quantite`.
Lis le **nom** d'une propriété sur l'entrée, puis affiche le **nom de son type** grâce à la
**réflexion**.

Exemple : `Nom` → `String` ; `Prix` → `Double` ; `Quantite` → `Int32`.

## Livrable

- `TypePropriete.cs`

## Indices

- `typeof(Produit)` renvoie l'objet `Type` qui décrit la classe.
- `GetProperty(nom)` renvoie un `PropertyInfo` (ajoute `!` car le résultat est nullable).
- La propriété `PropertyType` est le `Type` de la propriété ; `.Name` en donne le nom court
  (`String`, `Double`, `Int32`...).
- La réflexion exige `using System.Reflection;`.
