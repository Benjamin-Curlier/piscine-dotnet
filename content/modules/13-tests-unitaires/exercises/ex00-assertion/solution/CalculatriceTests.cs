using Xunit;

public class CalculatriceTests
{
    [Fact]
    public void Additionner_DeuxEntiers_RenvoieLeurSomme()
    {
        Assert.Equal(7, Calculatrice.Additionner(3, 4));
    }

    [Fact]
    public void Additionner_UnNegatif_ConserveLeSigne()
    {
        Assert.Equal(-2, Calculatrice.Additionner(3, -5));
    }
}
