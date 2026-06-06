using Xunit;

public class CompteTests
{
    [Fact]
    public void Exemple_ARemplacer()
    {
        // Remplace par de vrais tests du contrat de Compte.
        Assert.True(new Compte().Retirer(40));
    }
}
