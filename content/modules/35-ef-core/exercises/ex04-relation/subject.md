# ex04-relation — Relation 1-N & Include (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Les vraies bases ont des **relations**. Ici, un `Auteur` a plusieurs `Livre` (relation
**un-à-plusieurs**). EF Core déduit la clé étrangère par convention, et `Include` charge les
entités liées.

Lis `N` puis `N` lignes `auteur titre`. Un même auteur peut revenir plusieurs fois. Affiche, **par
auteur trié**, ses titres **triés**, sous la forme `auteur: titre1, titre2`.

Exemple : `3` / `Hugo Miserables` / `Hugo Notre-Dame` / `Zola Germinal` →
```
Hugo: Miserables, Notre-Dame
Zola: Germinal
```

## Livrable

- `Bibliotheque.cs`

## Contraintes

- Relation 1-N : `Auteur` détient une `List<Livre>` ; `Livre` a `AuteurId` + `Auteur`.
- Réutilise un auteur déjà créé plutôt que d'en dupliquer un.
- Charge les livres liés avec `Include`, et trie auteurs **et** titres.

## Indices

- Mémorise les auteurs déjà vus dans un `Dictionary<string, Auteur>`.
- Variante EF Core : `db.Auteurs.Local.FirstOrDefault(a => a.Nom == nomAuteur)` cherche parmi les
  entités déjà suivies, même avant `SaveChanges()`. `Local` ne représente pas toute la base : utilise-le
  ici uniquement parce que les auteurs viennent d'être ajoutés au même `DbContext`.
- `db.Auteurs.Include(a => a.Livres).OrderBy(a => a.Nom)` ; pour chaque auteur,
  `string.Join(", ", auteur.Livres.OrderBy(l => l.Titre).Select(l => l.Titre))`.
- EF crée automatiquement la table `Livre` avec la colonne `AuteurId` (convention de nommage).
