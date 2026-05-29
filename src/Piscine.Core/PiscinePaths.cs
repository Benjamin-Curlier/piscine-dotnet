using System.IO;

namespace Piscine.Core;

/// <summary>
/// Localise les dossiers de contenu pédagogique sous une racine donnée.
/// </summary>
public sealed class PiscinePaths
{
    public PiscinePaths(string contentRoot)
    {
        ContentRoot = contentRoot;
    }

    public string ContentRoot { get; }

    public string ModulesDirectory => Path.Combine(ContentRoot, "modules");

    public string RushesDirectory => Path.Combine(ContentRoot, "rushes");
}
