# ex03-groupby — Agrégation (GroupBy)

## Objectif

`GroupBy` regroupe les entités par une clé ; combiné à un agrégat comme `Count()`, il répond aux
questions « combien par catégorie ? ». EF Core le traduit en `GROUP BY` SQL.

Lis `N` puis `N` lignes `categorie nom`. Affiche, **par catégorie triée**, le nombre d'articles :
`categorie: nombre`.

Exemple : `4` / `fruit Pomme` / `legume Carotte` / `fruit Poire` / `legume Chou` →
```
fruit: 2
legume: 2
```

## Livrable

- `Catalogue.cs`

## Contraintes

- Regroupement côté requête, trié par clé pour le déterminisme.

## Indices

- `db.Articles.GroupBy(a => a.Categorie).Select(g => new { Categorie = g.Key, Nombre = g.Count() }).OrderBy(x => x.Categorie)`.
- Projette le groupe dans un type anonyme `{ clé, compte }` avant d'itérer : plus sûr à traduire en SQL.
