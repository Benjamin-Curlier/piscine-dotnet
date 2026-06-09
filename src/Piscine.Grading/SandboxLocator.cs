using System;
using System.IO;

namespace Piscine.Grading;

/// <summary>
/// Localise l'exécutable du bac à sable (<c>Piscine.Sandbox</c>/<c>.exe</c>). Modèle identique à
/// <c>Piscine.App.Terminal.ShimLocator</c> (le shim git). Cherche, dans l'ordre :
/// <list type="number">
///   <item><b>Surcharge</b> : variable d'env <c>PISCINE_SANDBOX</c> (chemin complet, autoritaire).</item>
///   <item><b>Co-localisé</b> : <c>&lt;BaseDirectory&gt;/Piscine.Sandbox(.exe)</c> — cas des tests
///   (<c>Piscine.Grading.Tests</c> référence l'exe, framework-dependent).</item>
///   <item><b>Packagé</b> : <c>&lt;ancêtre&gt;/sandbox/Piscine.Sandbox(.exe)</c> en remontant les
///   dossiers parents — publié <b>self-contained</b> par <c>release.yml</c> dans un sous-dossier
///   <c>sandbox/</c> (atteint depuis la CLI à la racine du bundle ET le Desktop en sous-dossier).</item>
///   <item><b>Dev</b> : remonte jusqu'à <c>Piscine.slnx</c> puis
///   <c>src/Piscine.Sandbox/bin/&lt;config&gt;/net10.0</c> (Release prioritaire, sinon Debug).</item>
/// </list>
/// <c>null</c> si introuvable → le client lève <see cref="SandboxUnavailableException"/> (fail-closed).
/// </summary>
internal static class SandboxLocator
{
    /// <summary>Résout depuis <see cref="AppContext.BaseDirectory"/> (dossier de l'exe en cours).</summary>
    public static string? Resolve() => Resolve(AppContext.BaseDirectory);

    /// <summary>Résout depuis un dossier de base donné (surchargé pour les tests).</summary>
    public static string? Resolve(string baseDirectory)
    {
        var overridePath = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath; // autoritaire : si invalide, le lancement lèvera (pas de repli).
        }

        var exeName = OperatingSystem.IsWindows() ? "Piscine.Sandbox.exe" : "Piscine.Sandbox";

        // (2) Co-localisé : exe à côté du binaire en cours (tests).
        var colocated = Path.Combine(baseDirectory, exeName);
        if (File.Exists(colocated))
        {
            return colocated;
        }

        // (3) Packagé : sous-dossier sandbox/ dans BaseDirectory ou un ancêtre (CLI à la racine,
        //     Desktop un cran plus bas → remonte le trouver).
        var dir = new DirectoryInfo(baseDirectory);
        for (var levels = 0; dir is not null && levels < 6; levels++, dir = dir.Parent)
        {
            var packaged = Path.Combine(dir.FullName, "sandbox", exeName);
            if (File.Exists(packaged))
            {
                return packaged;
            }
        }

        // (4) Dev : remonter jusqu'à Piscine.slnx puis la sortie de build du bac à sable.
        var root = new DirectoryInfo(baseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Piscine.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            return null;
        }

        foreach (var config in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(root.FullName, "src", "Piscine.Sandbox", "bin", config, "net10.0", exeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
