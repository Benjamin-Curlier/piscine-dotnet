using Xunit;

public class AccesTests
{
    [Fact]
    public void Classer_AgeAdulte_RenvoieMajeur()
    {
        // Ce cas ne suffit pas à sécuriser la frontière exacte de 18 ans.
        Assert.Equal("majeur", Acces.Classer(30));
    }
}
