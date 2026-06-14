using System;
using System.IO;

namespace Piscine.App.Launch;

/// <summary>Teste si une commande est résoluble via le PATH (ajoute .exe/.cmd/.bat sous Windows).</summary>
public static class ExecutableProbe
{
    public static bool OnPath(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var exts = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", ".bat", "" } : new[] { "" };
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            foreach (var ext in exts)
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, command + ext)))
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                    // dir contient des caractères invalides — ignorer
                }
            }
        }

        return false;
    }
}
