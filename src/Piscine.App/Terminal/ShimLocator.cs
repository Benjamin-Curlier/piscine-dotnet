using System;
using System.IO;

namespace Piscine.App.Terminal;

/// <summary>
/// Localise le dossier du shim git (<c>git</c>/<c>git.exe</c>, <c>AssemblyName=git</c>) à placer en tête
/// de PATH pour le coaching. Cherche, dans l'ordre :
/// <list type="number">
///   <item><b>App packagée</b> : <c>&lt;AppContext.BaseDirectory&gt;/gitshim/</c> (publié par release.yml à
///   côté de l'exe desktop).</item>
///   <item><b>Dev</b> : remonte jusqu'à <c>Piscine.slnx</c> puis
///   <c>src/Piscine.GitShim/bin/&lt;config&gt;/net10.0</c> (Release prioritaire, sinon Debug).</item>
/// </list>
/// <c>null</c> si le shim est introuvable → le terminal fonctionne nu, sans coaching de commande.
/// </summary>
public static class ShimLocator
{
    /// <summary>Résout depuis <see cref="AppContext.BaseDirectory"/> (dossier de l'exe en cours).</summary>
    public static string? Resolve() => Resolve(AppContext.BaseDirectory);

    /// <summary>Résout depuis un dossier de base donné (surchargé pour les tests).</summary>
    public static string? Resolve(string baseDirectory)
    {
        var gitName = OperatingSystem.IsWindows() ? "git.exe" : "git";

        // (a) App packagée : shim publié à côté de l'exe, dans gitshim/.
        var packaged = Path.Combine(baseDirectory, "gitshim");
        if (File.Exists(Path.Combine(packaged, gitName)))
        {
            return packaged;
        }

        // (b) Dev : remonter jusqu'à Piscine.slnx puis chercher la sortie de build du shim.
        var dir = new DirectoryInfo(baseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        foreach (var config in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(dir.FullName, "src", "Piscine.GitShim", "bin", config, "net10.0");
            if (File.Exists(Path.Combine(candidate, gitName)))
            {
                return candidate;
            }
        }

        return null;
    }
}
