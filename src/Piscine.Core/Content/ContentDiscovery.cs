using System.Collections.Generic;
using System.IO;
using System.Linq;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Découvre les modules sous <c>content/modules</c>, triés par <c>order</c>.</summary>
public static class ContentDiscovery
{
    public static IReadOnlyList<Module> DiscoverModules(PiscinePaths paths)
    {
        var modulesDir = paths.ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return new List<Module>();
        }

        return Directory.EnumerateDirectories(modulesDir)
            .Where(d => File.Exists(Path.Combine(d, ModuleLoader.FileName)))
            .Select(ModuleLoader.Load)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id, System.StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Découvre les Rushes sous <c>content/rushes</c>, triés par id (r0, r1, …).</summary>
    public static IReadOnlyList<Rush> DiscoverRushes(PiscinePaths paths)
    {
        var rushesDir = paths.RushesDirectory;
        if (!Directory.Exists(rushesDir))
        {
            return new List<Rush>();
        }

        return Directory.EnumerateDirectories(rushesDir)
            .Where(d => File.Exists(Path.Combine(d, ExerciseManifestLoader.FileName)))
            .Select(d =>
            {
                var manifest = ExerciseManifestLoader.Load(d);
                return new Rush(manifest.Id, manifest.Title, d);
            })
            .OrderBy(r => r.Id, System.StringComparer.Ordinal)
            .ToList();
    }
}
