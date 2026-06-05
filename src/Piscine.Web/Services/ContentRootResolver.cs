namespace Piscine.Web.Services;

/// <summary>Localise le dossier <c>content/</c> du dépôt (modules + rushes).</summary>
public static class ContentRootResolver
{
    /// <summary>
    /// Résolution, dans l'ordre : <c>PISCINE_CONTENT</c> (config ou variable d'environnement),
    /// puis remontée des dossiers depuis le répertoire courant et le binaire jusqu'à trouver
    /// un <c>content/modules</c>.
    /// </summary>
    public static string Resolve(IConfiguration config)
    {
        var configured = config["PISCINE_CONTENT"]
            ?? Environment.GetEnvironmentVariable("PISCINE_CONTENT");
        if (!string.IsNullOrWhiteSpace(configured) && HasModules(configured))
        {
            return Path.GetFullPath(configured);
        }

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "content");
                if (HasModules(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new DirectoryNotFoundException(
            "Dossier 'content' introuvable. Lancez le site depuis le dépôt, " +
            "ou définissez PISCINE_CONTENT vers le dossier content/.");
    }

    private static bool HasModules(string contentRoot)
        => Directory.Exists(Path.Combine(contentRoot, "modules"));
}
