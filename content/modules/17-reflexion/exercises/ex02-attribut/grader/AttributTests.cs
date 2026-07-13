using System;
using System.Reflection;
using Xunit;

// La cible définie ICI porte l'attribut de la recrue avec un AUTRE texte : prouve que l'attribut
// transporte une valeur arbitraire (pas un simple Console.Write de "Coucou").
[Etiquette("Zêta")]
class CibleTest { }

public class AttributTests
{
    [Fact]
    public void EtiquetteAttribute_DeriveDeAttribute()
    {
        Assert.True(
            typeof(Attribute).IsAssignableFrom(typeof(EtiquetteAttribute)),
            "EtiquetteAttribute doit hériter de System.Attribute.");
    }

    [Fact]
    public void MaClasse_PorteLEtiquetteCoucou()
    {
        var etiquette = typeof(MaClasse).GetCustomAttribute<EtiquetteAttribute>();
        Assert.NotNull(etiquette);
        Assert.Equal("Coucou", etiquette.Texte);
    }

    [Fact]
    public void LEtiquette_TransporteUnTexteArbitraire()
    {
        var etiquette = typeof(CibleTest).GetCustomAttribute<EtiquetteAttribute>();
        Assert.NotNull(etiquette);
        Assert.Equal("Zêta", etiquette.Texte);
    }
}
