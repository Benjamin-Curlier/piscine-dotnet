# ex02-where — Filtre (Where) + tri

## Objectif

`Where` filtre les entités selon une condition ; EF Core la traduit en clause `WHERE` SQL. Combiné
à `OrderBy`, on obtient une requête ciblée et déterministe.

Lis un `seuil`, puis `N`, puis `N` lignes `nom prix`. Affiche, **triés par nom**, les produits dont
le prix est **supérieur ou égal** au seuil.

Exemple : `2` / `3` / `Poire 2` / `Pomme 1` / `Banane 3` →
```
Banane 3
Poire 2
```
(Pomme à 1 est sous le seuil.)

## Livrable

- `Catalogue.cs`

## Contraintes

- Filtre côté requête : `db.Produits.Where(x => x.Prix >= seuil)`.
- Trie ensuite par nom.

## Indices

- On peut chaîner : `db.Produits.Where(...).OrderBy(...)`.
- Attention à l'ordre de lecture de l'entrée : seuil, puis N, puis les lignes.
