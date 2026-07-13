using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Découvre les modules sous <c>content/modules</c>, triés par <c>order</c>.</summary>
public static class ContentDiscovery
{
    /// <summary>
    /// Découvre les modules. Résilient : un <c>module.yaml</c> illisible/malformé est ignoré au lieu de
    /// faire planter <c>list</c>/<c>status</c>/<c>validate-content</c> (un poste recrue ne doit jamais
    /// voir de stack trace). La validation stricte de ces fichiers relève de <c>validate-content</c>
    /// (cf. <c>ContentValidator</c>), qui les signale explicitement — pas de fail-open silencieux.
    /// </summary>
    public static IReadOnlyList<Module> DiscoverModules(PiscinePaths paths)
    {
        var modulesDir = paths.ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return new List<Module>();
        }

        return Directory.EnumerateDirectories(modulesDir)
            .Where(d => File.Exists(Path.Combine(d, ModuleLoader.FileName)))
            .Select(TryLoadModule)
            .OfType<Module>()
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static Module? TryLoadModule(string moduleDir)
    {
        try
        {
            return ModuleLoader.Load(moduleDir);
        }
        catch (Exception)
        {
            return null; // signalé par validate-content ; ignoré ici pour ne pas casser l'affichage.
        }
    }

    /// <summary>Découvre les Rushes sous <c>content/rushes</c>, triés par id (r0, r1, …). Résilient (cf. modules).</summary>
    public static IReadOnlyList<Rush> DiscoverRushes(PiscinePaths paths)
    {
        var rushesDir = paths.RushesDirectory;
        if (!Directory.Exists(rushesDir))
        {
            return new List<Rush>();
        }

        return Directory.EnumerateDirectories(rushesDir)
            .Where(d => File.Exists(Path.Combine(d, ExerciseManifestLoader.FileName)))
            .Select(TryLoadRush)
            .OfType<Rush>()
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static Rush? TryLoadRush(string rushDir)
    {
        try
        {
            var manifest = ExerciseManifestLoader.Load(rushDir);
            return new Rush(manifest.Id, manifest.Title, rushDir);
        }
        catch (Exception)
        {
            return null; // signalé par validate-content ; ignoré ici pour ne pas casser l'affichage.
        }
    }
}
