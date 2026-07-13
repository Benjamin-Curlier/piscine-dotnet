# ex02-attribut — Un attribut personnalisé

## Objectif

Définis un **attribut personnalisé** `EtiquetteAttribute` qui transporte un texte. Applique-le
sur **plusieurs classes**, chacune avec un texte **différent** (par ex. `[Etiquette("Coucou")]`
sur `MaClasse`).

Lis sur l'entrée standard le **nom d'une classe** (`MaClasse`, `Produit` ou `Client`), sélectionne
le **type** correspondant, puis **lis** le texte de son attribut par réflexion et affiche-le.

Exemple : pour l'entrée `Produit`, la sortie est `Catalogue de produits`.

## Livrable

- `Attribut.cs`

## Indices

- Un attribut est une classe qui hérite de `Attribute`. Par convention, son nom se termine par
  `Attribute` (mais on l'écrit `[Etiquette(...)]` sans le suffixe).
- Donne-lui un constructeur prenant un `string` et stocke-le dans une propriété `Texte`.
- Applique l'attribut sur plusieurs classes, avec un texte différent : `[Etiquette("Coucou")]` sur
  `MaClasse`, `[Etiquette("Catalogue de produits")]` sur `Produit`, etc.
- Lis le nom sur `System.Console.ReadLine()`, choisis le type avec un `switch` renvoyant
  `typeof(...)`, puis lis `type.GetCustomAttribute<EtiquetteAttribute>()!.Texte`.
- `Attribute` exige `using System;` ; `GetCustomAttribute<T>()` exige `using System.Reflection;`.
