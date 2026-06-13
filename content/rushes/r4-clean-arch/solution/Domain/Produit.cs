namespace Domain;

// Entité du domaine : un produit du catalogue.
// Aucune dépendance vers Application ni Infrastructure.
public sealed class Produit
{
    public Produit(int id, string nom, decimal prix)
    {
        Id = id;
        Nom = nom;
        Prix = prix;
    }

    public int Id { get; }

    public string Nom { get; }

    public decimal Prix { get; private set; }

    public void MettreAJourPrix(decimal nouveauPrix) => Prix = nouveauPrix;
}
