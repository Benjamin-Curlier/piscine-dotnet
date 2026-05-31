# ex02-attribut — Un attribut personnalisé

## Objectif

Définis un **attribut personnalisé** `EtiquetteAttribute` qui transporte un texte. Applique-le
sur une classe `MaClasse` avec `[Etiquette("Coucou")]`, puis **lis** ce texte par réflexion et
affiche-le.

Il n'y a **rien à lire** sur l'entrée.

Exemple de sortie : `Coucou`.

## Livrable

- `Attribut.cs`

## Indices

- Un attribut est une classe qui hérite de `Attribute`. Par convention, son nom se termine par
  `Attribute` (mais on l'écrit `[Etiquette(...)]` sans le suffixe).
- Donne-lui un constructeur prenant un `string` et stocke-le dans une propriété `Texte`.
- Applique l'attribut sur `MaClasse` : `[Etiquette("Coucou")]`.
- Lis-le avec `typeof(MaClasse).GetCustomAttribute<EtiquetteAttribute>()!.Texte`.
- `Attribute` exige `using System;` ; `GetCustomAttribute<T>()` exige `using System.Reflection;`.
