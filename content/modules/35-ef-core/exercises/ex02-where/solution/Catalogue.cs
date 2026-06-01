using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var seuil = int.Parse(System.Console.ReadLine());
var n = int.Parse(System.Console.ReadLine());
var lignes = new string[n];
for (var i = 0; i < n; i++)
{
    lignes[i] = System.Console.ReadLine();
}

var connexion = new SqliteConnection("DataSource=:memory:");
connexion.Open();
var options = new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options;

using var db = new Catalogue(options);
db.Database.EnsureCreated();

foreach (var ligne in lignes)
{
    var p = ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    db.Produits.Add(new Produit { Nom = p[0], Prix = int.Parse(p[1]) });
}
db.SaveChanges();

// WHERE (filtre) + ORDER BY (déterminisme).
var chers = db.Produits.Where(x => x.Prix >= seuil).OrderBy(x => x.Nom);
foreach (var produit in chers)
{
    System.Console.WriteLine(produit.Nom + " " + produit.Prix);
}

class Produit
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Prix { get; set; }
}

class Catalogue : DbContext
{
    public Catalogue(DbContextOptions<Catalogue> options) : base(options) { }
    public DbSet<Produit> Produits => Set<Produit>();
}
