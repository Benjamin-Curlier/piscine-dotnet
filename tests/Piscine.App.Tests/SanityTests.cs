using System.Reflection;

namespace Piscine.App.Tests;

public class SanityTests
{
    [Fact]
    public void Engine_assemblies_are_resolvable_through_App()
    {
        // Garde-fou : Piscine.App est la couche services au-dessus du moteur ; ses
        // dépendances (Core/Grading/Git) doivent être résolvables à l'exécution. Si le
        // chaînage de références casse, Assembly.Load lève et le test échoue réellement.
        _ = new Piscine.App.AppMarker(); // ancre l'assembly Piscine.App dans le contexte

        foreach (var name in new[] { "Piscine.Core", "Piscine.Grading", "Piscine.Git" })
        {
            Assert.Equal(name, Assembly.Load(name).GetName().Name);
        }
    }
}
