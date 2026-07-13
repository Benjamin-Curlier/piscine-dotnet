using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Tests cachés (grader unit) : « Réussi » exige les BONNES durées de vie dans le conteneur DI —
// pas quatre Console.WriteLine des valeurs attendues.
public class DureesVieTests
{
    [Fact]
    public void Singleton_MemeInstanceAChaqueResolution()
    {
        var provider = Conteneur.Construire();

        var a = provider.GetRequiredService<CompteurSingleton>();
        var b = provider.GetRequiredService<CompteurSingleton>();

        Assert.Same(a, b);
    }

    [Fact]
    public void Transient_InstanceNeuveAChaqueResolution()
    {
        var provider = Conteneur.Construire();

        var a = provider.GetRequiredService<CompteurTransient>();
        var b = provider.GetRequiredService<CompteurTransient>();

        Assert.NotSame(a, b);
    }
}
