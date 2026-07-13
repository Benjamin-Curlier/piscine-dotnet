using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis N puis N noms de produits. Crée un DbContext sur SQLite en mémoire, insère les
// produits, puis affiche leur nombre.
// IMPORTANT : ouvre la SqliteConnection ("DataSource=:memory:") et GARDE-la ouverte,
// sinon la base disparaît.

var n = int.Parse(System.Console.ReadLine());

// TODO : lis les noms, ouvre la connexion, configure UseSqlite, EnsureCreated,
//        Add + SaveChanges, puis affiche Produits.Count().

// TODO : classe Produit { Id, Nom } et classe Catalogue : DbContext { DbSet<Produit> Produits }.
