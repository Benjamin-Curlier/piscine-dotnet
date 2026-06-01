# ex00-insert — DbContext, insertion & comptage

## Objectif

**Entity Framework Core** (EF Core) est l'ORM de .NET : il fait correspondre des **classes** à des
**tables** et te laisse manipuler la base en C#, sans écrire de SQL. Ici, on utilise **SQLite en
mémoire**, idéal pour apprendre (rien à installer, base jetable).

Lis un entier `N`, puis `N` noms de produits. Insère-les en base, puis affiche le **nombre** de
produits enregistrés.

Exemple : `3` / `Pomme` / `Poire` / `Banane` → `3`.

## Livrable

- `Catalogue.cs`

## Contraintes

- Base **SQLite in-memory** ; la `SqliteConnection` doit rester **ouverte** tout le programme.
- Une classe entité `Produit` et un `DbContext` exposant un `DbSet<Produit>`.

## Indices

- `var connexion = new SqliteConnection("DataSource=:memory:"); connexion.Open();`
- `new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options`.
- `db.Database.EnsureCreated();` crée le schéma. Puis `db.Produits.Add(...)`, `db.SaveChanges()`,
  et `db.Produits.Count()`.
- Usings nécessaires : `System.Linq`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore`.
