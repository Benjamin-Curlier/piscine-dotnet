using System.Linq;
using Xunit;

// Tests cachés (grader unit) : la correction appelle ListerProprietes sur un type CACHÉ (Client) —
// impossible de coder en dur les noms de Produit, il faut vraiment employer la réflexion.
public class ListerProprietesTests
{
    private sealed class Client
    {
        public string Ville { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Adresse { get; set; } = string.Empty;
    }

    [Fact]
    public void ListerProprietes_RenvoieLesNomsTries_PourUnTypeCache()
    {
        var noms = Reflexion.ListerProprietes(typeof(Client)).ToArray();

        Assert.Equal(new[] { "Adresse", "Age", "Ville" }, noms);
    }

    [Fact]
    public void ListerProprietes_FonctionnePourProduit()
    {
        var noms = Reflexion.ListerProprietes(typeof(Produit)).ToArray();

        Assert.Equal(new[] { "Nom", "Prix", "Quantite" }, noms);
    }
}
