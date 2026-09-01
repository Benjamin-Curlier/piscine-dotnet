using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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
    db.Articles.Add(new Article { Categorie = p[0], Nom = p[1] });
}
db.SaveChanges();

// GROUP BY + agrégat, trié par catégorie pour rester déterministe.
var stats = db.Articles
    .GroupBy(a => a.Categorie)
    .Select(g => new { Categorie = g.Key, Nombre = g.Count() })
    .OrderBy(x => x.Categorie);

foreach (var s in stats)
{
    System.Console.WriteLine(s.Categorie + ": " + s.Nombre);
}

class Article
{
    public int Id { get; set; }
    public string Categorie { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
}

class Catalogue : DbContext
{
    public Catalogue(DbContextOptions<Catalogue> options) : base(options) { }
    public DbSet<Article> Articles => Set<Article>();
}
