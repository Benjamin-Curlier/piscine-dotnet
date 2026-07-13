using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var n = int.Parse(System.Console.ReadLine());
var noms = new string[n];
for (var i = 0; i < n; i++)
{
    noms[i] = System.Console.ReadLine();
}

// SQLite en mémoire : la connexion doit RESTER ouverte pour que la base survive.
var connexion = new SqliteConnection("DataSource=:memory:");
connexion.Open();
var options = new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options;

using var db = new Catalogue(options);
db.Database.EnsureCreated();

foreach (var nom in noms)
{
    db.Produits.Add(new Produit { Nom = nom });
}
db.SaveChanges();

// Nombre réellement enregistré, relu depuis la base (pas depuis N).
System.Console.WriteLine(db.Produits.Count());

// Noms triés, obtenus par une requête EF Core (ORDER BY exécuté par SQLite).
foreach (var nom in db.Produits.OrderBy(p => p.Nom).Select(p => p.Nom))
{
    System.Console.WriteLine(nom);
}

class Produit
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
}

class Catalogue : DbContext
{
    public Catalogue(DbContextOptions<Catalogue> options) : base(options) { }
    public DbSet<Produit> Produits => Set<Produit>();
}
