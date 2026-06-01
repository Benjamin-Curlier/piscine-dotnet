# ex01-orderby — Requête triée (OrderBy)

## Objectif

Une requête EF Core se construit avec LINQ. Pour obtenir une sortie **déterministe** (toujours le
même ordre), il faut **trier explicitement** avec `OrderBy`.

Lis `N` puis `N` lignes `nom prix`. Insère les produits, puis affiche-les **triés par nom**, une
ligne `nom prix` par produit.

Exemple : `3` / `Poire 2` / `Pomme 1` / `Banane 3` →
```
Banane 3
Poire 2
Pomme 1
```

## Livrable

- `Catalogue.cs`

## Contraintes

- Le tri se fait côté requête : `db.Produits.OrderBy(x => x.Nom)`.
- Sans `OrderBy`, l'ordre des lignes n'est pas garanti — l'exercice exige le tri.

## Indices

- L'entité `Produit` a maintenant un champ `Prix`.
- `foreach (var p in db.Produits.OrderBy(x => x.Nom)) System.Console.WriteLine(p.Nom + " " + p.Prix);`.
