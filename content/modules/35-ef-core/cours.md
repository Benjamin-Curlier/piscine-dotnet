# Module 35 — Entity Framework Core (SQLite in-memory)

La plupart des applications ont besoin de **stocker des données**. Plutôt que d'écrire du SQL à la
main et de convertir manuellement les lignes en objets, on utilise un **ORM** (*Object-Relational
Mapper*). En .NET, c'est **Entity Framework Core** : tu décris tes données par des **classes**, et
EF Core s'occupe des tables, des requêtes et du suivi des changements.

Ce module utilise **SQLite en mémoire** : une vraie base SQL, mais éphémère et sans installation —
parfaite pour apprendre et pour les tests.

---

## 1. Les briques d'EF Core {#dbcontext}

- **Entité** : une classe simple (POCO) dont les propriétés deviennent des colonnes. Une propriété
  `Id` sert de clé primaire par convention.
- **`DbContext`** : la session avec la base. Il expose des **`DbSet<T>`** (une par table) et suit
  les modifications.

```csharp
class Produit
{
    public int Id { get; set; }            // clé primaire (convention)
    public string Nom { get; set; } = string.Empty;
}

class Catalogue : DbContext
{
    public Catalogue(DbContextOptions<Catalogue> options) : base(options) { }
    public DbSet<Produit> Produits => Set<Produit>();
}
```

### Brancher SQLite in-memory

La base en mémoire vit **tant que la connexion est ouverte**. On ouvre donc explicitement une
`SqliteConnection` et on la garde ouverte :

```csharp
var connexion = new SqliteConnection("DataSource=:memory:");
connexion.Open();                                   // NE PAS fermer avant la fin
var options = new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options;

using var db = new Catalogue(options);
db.Database.EnsureCreated();                         // crée le schéma à partir des entités
```

Usings : `Microsoft.EntityFrameworkCore`, `Microsoft.Data.Sqlite`, `System.Linq`.

---

## 2. Écrire : Add / SaveChanges

On ajoute des entités au `DbSet`, puis on **valide** en une transaction avec `SaveChanges()` :

```csharp
db.Produits.Add(new Produit { Nom = "Pomme" });
db.SaveChanges();           // INSERT réellement exécuté ici
```

EF affecte automatiquement l'`Id` généré après l'insertion.

---

## 3. Lire : LINQ → SQL {#requetes}

Les requêtes s'écrivent en **LINQ** et sont traduites en SQL exécuté par la base.

```csharp
db.Produits.Count();                          // SELECT COUNT(*)
db.Produits.Where(p => p.Prix >= 2);          // WHERE prix >= 2
db.Produits.OrderBy(p => p.Nom);              // ORDER BY nom
```

> ⚠️ **Déterminisme** : une base ne garantit **aucun ordre** sans `ORDER BY`. Pour une sortie
> reproductible (et donc corrigeable), **trie toujours** explicitement tes résultats. C'est la
> règle d'or de ce module.

---

## 4. Agréger : GroupBy {#agregation}

```csharp
var stats = db.Articles
    .GroupBy(a => a.Categorie)
    .Select(g => new { Categorie = g.Key, Nombre = g.Count() })
    .OrderBy(x => x.Categorie);
```

Projeter le groupe dans un type anonyme `{ clé, agrégat }` avant d'itérer aide EF à produire un
`GROUP BY` SQL propre.

---

## 5. Relations : 1-N & Include {#relations}

Une entité peut **référencer** d'autres entités. Pour une relation un-à-plusieurs, le côté « un »
détient une **collection**, le côté « plusieurs » une **clé étrangère** :

```csharp
class Auteur
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public List<Livre> Livres { get; } = new();   // côté « plusieurs »
}

class Livre
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int AuteurId { get; set; }              // clé étrangère (convention)
    public Auteur Auteur { get; set; } = null!;
}
```

EF déduit la relation par **convention** (`AuteurId` → `Auteur`). Pour charger les entités liées
dans une même requête, on utilise **`Include`** :

```csharp
foreach (var a in db.Auteurs.Include(x => x.Livres).OrderBy(x => x.Nom))
{
    // a.Livres est rempli
}
```

Sans `Include`, la collection `Livres` ne serait pas chargée (chargement « paresseux » désactivé
par défaut).

---

## 6. En pratique {#pratique}

- Une entité = une classe POCO avec un `Id` ; un `DbContext` = des `DbSet<T>`.
- `EnsureCreated()` pour un schéma jetable (en prod, on utilise plutôt les **migrations**).
- **Toujours `OrderBy`** pour une sortie déterministe.
- SQLite in-memory : garder la connexion ouverte le temps du programme.

### Exercices du module

- **ex00-insert** — DbContext, insertion & comptage.
- **ex01-orderby** — requête triée.
- **ex02-where** — filtre + tri.
- **ex03-groupby** — agrégation par groupe.
- **ex04-relation** *(bonus)* — relation 1-N & `Include`.

## Références externes

- [Entity Framework Core (doc Microsoft)](https://learn.microsoft.com/ef/core/)
- [EF Core + SQLite](https://learn.microsoft.com/ef/core/providers/sqlite/)
- [Requêtes LINQ avec EF Core](https://learn.microsoft.com/ef/core/querying/)
