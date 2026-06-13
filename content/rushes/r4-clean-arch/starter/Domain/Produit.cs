namespace Domain;

// Couche DOMAIN : l'entité métier. Aucune dépendance vers Application ni Infrastructure.
// TODO : id (int, lecture seule), nom (string, lecture seule), prix (decimal, modifiable),
//        et MettreAJourPrix(decimal).
public sealed class Produit
{
    public Produit(int id, string nom, decimal prix)
    {
        // TODO : initialiser Id, Nom et Prix.
    }

    // TODO : propriétés Id, Nom, Prix et méthode MettreAJourPrix(decimal nouveauPrix).
}
