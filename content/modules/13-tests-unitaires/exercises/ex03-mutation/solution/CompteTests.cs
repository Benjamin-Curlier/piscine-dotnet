using Xunit;

public class CompteTests
{
    [Fact]
    public void Retirer_MontantInferieur_Reussit_EtDebiteLeSolde()
    {
        var compte = new Compte();
        Assert.True(compte.Retirer(40));
        Assert.Equal(60, compte.Solde);
    }

    [Fact]
    public void Retirer_MontantEgalAuSolde_Reussit()
    {
        Assert.True(new Compte().Retirer(100));
    }

    [Fact]
    public void Retirer_MontantSuperieur_Echoue()
    {
        Assert.False(new Compte().Retirer(101));
    }
}
