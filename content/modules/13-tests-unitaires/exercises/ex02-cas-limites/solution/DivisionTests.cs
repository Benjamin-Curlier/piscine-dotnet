using System;
using Xunit;

public class DivisionTests
{
    [Fact]
    public void Calculer_DivisionSimple_RenvoieLeQuotient()
    {
        Assert.Equal(2, Division.Calculer(6, 3));
    }

    [Fact]
    public void Calculer_DiviseurNul_LeveArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Division.Calculer(6, 0));
    }
}
