using System.Collections.Generic;
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
var options = new DbContextOptionsBuilder<Bibliotheque>().UseSqlite(connexion).Options;

using var db = new Bibliotheque(options);
db.Database.EnsureCreated();

// Relation 1-N : un Auteur a plusieurs Livres. On réutilise l'auteur déjà créé.
var auteurs = new Dictionary<string, Auteur>();
foreach (var ligne in lignes)
{
    var p = ligne.Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries);
    if (!auteurs.TryGetValue(p[0], out var auteur))
    {
        auteur = new Auteur { Nom = p[0] };
        auteurs[p[0]] = auteur;
        db.Auteurs.Add(auteur);
    }
    auteur.Livres.Add(new Livre { Titre = p[1] });
}
db.SaveChanges();

// Include charge les livres liés ; tout est trié pour le déterminisme.
foreach (var auteur in db.Auteurs.Include(a => a.Livres).OrderBy(a => a.Nom))
{
    var titres = string.Join(", ", auteur.Livres.OrderBy(l => l.Titre).Select(l => l.Titre));
    System.Console.WriteLine(auteur.Nom + ": " + titres);
}

class Auteur
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public List<Livre> Livres { get; } = new();
}

class Livre
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public int AuteurId { get; set; }
    public Auteur Auteur { get; set; } = null!;
}

class Bibliotheque : DbContext
{
    public Bibliotheque(DbContextOptions<Bibliotheque> options) : base(options) { }
    public DbSet<Auteur> Auteurs => Set<Auteur>();
    public DbSet<Livre> Livres => Set<Livre>();
}
