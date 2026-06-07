using System.IO;
using Piscine.App.Terminal;
using Xunit;

namespace Piscine.App.Tests;

/// <summary>Tests de <see cref="ShimLocator"/> : priorité à l'emplacement packagé ; null si introuvable.</summary>
public sealed class ShimLocatorTests
{
    private static string GitName => System.OperatingSystem.IsWindows() ? "git.exe" : "git";

    [Fact]
    public void Resolve_PrefersPackagedGitshimNextToExe()
    {
        using var dir = new TempDir();
        // Simule l'app packagée : <base>/gitshim/git[.exe].
        dir.WriteFile(Path.Combine("gitshim", GitName), "shim");

        var resolved = ShimLocator.Resolve(dir.Path);

        Assert.Equal(Path.Combine(dir.Path, "gitshim"), resolved);
    }

    [Fact]
    public void Resolve_WhenNoShimAndNoSolution_ReturnsNull()
    {
        using var dir = new TempDir();
        // Pas de gitshim/, et %TEMP% n'est pas sous le dépôt → aucun Piscine.slnx en remontant.
        Assert.Null(ShimLocator.Resolve(dir.Path));
    }
}

/// <summary>Tests de <see cref="TerminalPolicy"/> : le drapeau reflète l'argument du constructeur.</summary>
public sealed class TerminalPolicyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Enabled_ReflectsConstructorArgument(bool enabled)
    {
        Assert.Equal(enabled, new TerminalPolicy(enabled).Enabled);
    }
}
