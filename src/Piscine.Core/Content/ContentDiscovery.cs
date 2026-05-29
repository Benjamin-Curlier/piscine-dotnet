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
}
