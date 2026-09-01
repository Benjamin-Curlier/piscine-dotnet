using Xunit;

public class CalculatriceTests
{
    [Fact]
    public void Additionner_DeuxZeros_RenvoieZero()
    {
        Assert.Equal(0, Calculatrice.Additionner(0, 0));
    }
}
