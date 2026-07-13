# ex01-lister-proprietes — Lister les propriétés

## Objectif

Complète **`Reflexion.ListerProprietes(Type type)`** : renvoie le **nom de toutes les propriétés**
du type reçu, **triés par ordre alphabétique** (pour un résultat déterministe).

Le `Main` t'est **fourni** : il appelle ta méthode sur la classe `Produit` et affiche chaque nom,
un par ligne. Il n'y a **rien à lire** sur l'entrée.

Exemple de sortie pour `Produit` : `Nom`, puis `Prix`, puis `Quantite`.

## Livrable

- `ListerProprietes.cs`

## Indices

- `type.GetProperties()` renvoie un tableau de `PropertyInfo`.
- Chaque `PropertyInfo` a une propriété `Name`.
- L'ordre renvoyé par la réflexion n'est pas garanti : trie avec `OrderBy(n => n)` pour un
  résultat stable.
- `Select` et `OrderBy` exigent `using System.Linq;` ; la réflexion exige
  `using System.Reflection;` (déjà en haut).
- La correction appelle ta méthode sur un **autre type que `Produit`** : ne code aucun nom en dur,
  applique la réflexion sur le `type` reçu.
