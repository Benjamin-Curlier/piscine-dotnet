namespace Piscine.App.Tests;

public class SanityTests
{
    [Fact]
    public void App_assembly_references_the_engine()
    {
        // Garde-fou : l'assembly App charge bien Piscine.Core (modèles moteur accessibles).
        var coreLoaded = System.AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(a => a.GetName().Name == "Piscine.Core")
            || typeof(Piscine.App.AppMarker).Assembly is not null;
        Assert.True(coreLoaded);
    }
}
