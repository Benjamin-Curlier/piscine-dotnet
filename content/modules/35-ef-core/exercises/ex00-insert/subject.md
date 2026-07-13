# ex00-insert — DbContext, insertion & comptage

## Objectif

**Entity Framework Core** (EF Core) est l'ORM de .NET : il fait correspondre des **classes** à des
**tables** et te laisse manipuler la base en C#, sans écrire de SQL. Ici, on utilise **SQLite en
mémoire**, idéal pour apprendre (rien à installer, base jetable).

Lis un entier `N`, puis `N` noms de produits. Insère-les en base, puis affiche :

1. le **nombre** de produits enregistrés, **relu en base** (`Produits.Count()`, et non `N`) ;
2. les noms **triés par ordre alphabétique**, un par ligne, obtenus par une **requête EF Core**
   (`Produits.OrderBy(p => p.Nom)`) — pas en retriant l'entrée.

Exemple : `3` / `Pomme` / `Poire` / `Banane` →

```
3
Banane
Poire
Pomme
```

Comme la sortie repasse **par la base** (comptage puis tri), un simple écho de l'entrée ne suffit
pas : l'ordre des noms affichés ne suit pas l'ordre de saisie.

## Livrable

- `Catalogue.cs`

## Contraintes

- Base **SQLite in-memory** ; la `SqliteConnection` doit rester **ouverte** tout le programme.
- Une classe entité `Produit` et un `DbContext` exposant un `DbSet<Produit>`.

## Indices

- `var connexion = new SqliteConnection("DataSource=:memory:"); connexion.Open();`
- `new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options`.
- `db.Database.EnsureCreated();` crée le schéma. Puis `db.Produits.Add(...)`, `db.SaveChanges()`,
  `db.Produits.Count()`, et enfin `db.Produits.OrderBy(p => p.Nom).Select(p => p.Nom)` pour relire
  les noms triés.
- Usings nécessaires : `System.Linq`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore`.
