using System;
using System.IO;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// Couvre la résolution du binaire du bac à sable (<c>SandboxLocator</c>) et la préparation du
/// lancement (<c>SandboxLauncher</c>) — y compris le cas <c>null</c> (introuvable) qui pilote le
/// fail-closed côté client, et la branche <c>.dll</c> via le muxer <c>dotnet</c>. Types internes
/// accessibles via <c>InternalsVisibleTo</c>. Cf. audit COV-1 (SandboxLauncher 53,8 %).
/// (Parallélisation désactivée pour l'assembly → la manipulation de <c>PISCINE_SANDBOX</c> ne court pas.)
/// </summary>
public class SandboxLocatorTests
{
    [Fact]
    public void Resolve_HonorsPiscineSandboxOverride()
    {
        var prev = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", "/chemin/autoritaire/sb");
            Assert.Equal("/chemin/autoritaire/sb", SandboxLocator.Resolve("/n/importe/quoi"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", prev);
        }
    }

    [Fact]
    public void Resolve_FindsPackagedSandboxSubdir()
    {
        var prev = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        var tmp = Directory.CreateTempSubdirectory("sbx-loc-").FullName;
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", null);
            var exeName = OperatingSystem.IsWindows() ? "Piscine.Sandbox.exe" : "Piscine.Sandbox";
            Directory.CreateDirectory(Path.Combine(tmp, "sandbox"));
            var exe = Path.Combine(tmp, "sandbox", exeName);
            File.WriteAllText(exe, "stub");

            Assert.Equal(exe, SandboxLocator.Resolve(tmp));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", prev);
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNothingFound()
    {
        var prev = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        var tmp = Directory.CreateTempSubdirectory("sbx-none-").FullName;
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", null);

            // Dossier temp hors dépôt : ni co-localisé, ni sous-dossier sandbox/, ni ancêtre Piscine.slnx.
            Assert.Null(SandboxLocator.Resolve(tmp));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", prev);
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void CreateStartInfo_DllPath_GoesThroughDotnetMuxer()
    {
        var prev = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        try
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", "/depot/Piscine.Sandbox.dll");

            var psi = SandboxLauncher.CreateStartInfo("/work");

            Assert.Contains("dotnet", psi.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("/depot/Piscine.Sandbox.dll", psi.ArgumentList[0]);
            Assert.Equal("/work", psi.ArgumentList[1]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PISCINE_SANDBOX", prev);
        }
    }
}
