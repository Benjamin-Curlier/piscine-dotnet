using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

// Tests cachés (grader unit) : « Réussi » exige un VRAI DbContext EF Core (entité mappée, DbSet,
// round-trip SQLite) — pas un Console.Write du compte attendu.
public class InsertTests
{
    private static Catalogue NouveauContexte(SqliteConnection connexion) =>
        new Catalogue(new DbContextOptionsBuilder<Catalogue>().UseSqlite(connexion).Options);

    [Fact]
    public void Insertion_PersisteEnBase_EtCompte()
    {
        using var connexion = new SqliteConnection("DataSource=:memory:");
        connexion.Open();

        using (var db = NouveauContexte(connexion))
        {
            db.Database.EnsureCreated();
            db.Produits.Add(new Produit { Nom = "Pomme" });
            db.Produits.Add(new Produit { Nom = "Poire" });
            db.Produits.Add(new Produit { Nom = "Banane" });
            db.SaveChanges();
            Assert.Equal(3, db.Produits.Count());
        }

        // Nouveau contexte sur la MÊME connexion : prouve que les produits sont réellement en base.
        using (var relecture = NouveauContexte(connexion))
        {
            Assert.Equal(3, relecture.Produits.Count());
            Assert.Contains(relecture.Produits, p => p.Nom == "Poire");
        }
    }

    [Fact]
    public void BaseVide_Compte_Zero()
    {
        using var connexion = new SqliteConnection("DataSource=:memory:");
        connexion.Open();

        using var db = NouveauContexte(connexion);
        db.Database.EnsureCreated();

        Assert.Equal(0, db.Produits.Count());
    }
}
