using Xunit;

public class AccesTests
{
    [Fact]
    public void Classer_JusteAvantLaLimite_RenvoieMineur()
    {
        Assert.Equal("mineur", Acces.Classer(17));
    }

    [Fact]
    public void Classer_A_LaLimite_RenvoieMajeur()
    {
        Assert.Equal("majeur", Acces.Classer(18));
    }
}
