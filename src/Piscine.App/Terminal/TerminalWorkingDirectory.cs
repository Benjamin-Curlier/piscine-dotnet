using Piscine.Core;

namespace Piscine.App.Terminal;

/// <summary>Résout le dossier demandé par la page terminal sans sortir du workspace officiel.</summary>
public static class TerminalWorkingDirectory
{
    public static string Resolve(PiscineLayout layout, string? requestedDirectory)
    {
        var workspaceRoot = Path.GetFullPath(layout.WorkspaceRoot);
        if (string.IsNullOrWhiteSpace(requestedDirectory))
        {
            return workspaceRoot;
        }

        try
        {
            var requested = Path.GetFullPath(requestedDirectory);
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var rootPrefix = workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var isInsideWorkspace = string.Equals(requested, workspaceRoot, comparison)
                || requested.StartsWith(rootPrefix, comparison);

            return isInsideWorkspace && Directory.Exists(requested)
                ? requested
                : workspaceRoot;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return workspaceRoot;
        }
    }
}
