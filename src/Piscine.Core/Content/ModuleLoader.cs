using System.IO;
using Piscine.Core.Io;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Charge le <c>module.yaml</c> d'un dossier de module.</summary>
public static class ModuleLoader
{
    public const string FileName = "module.yaml";

    public static Module Load(string moduleDirectory)
    {
        var path = Path.Combine(moduleDirectory, FileName);
        return YamlLoader.Load<Module>(path);
    }
}
