using Xunit;

public class DivisionTests
{
    [Fact]
    public void Calculer_DivisionSimple_RenvoieLeQuotient()
    {
        Assert.Equal(2, Division.Calculer(6, 3));
    }
}
