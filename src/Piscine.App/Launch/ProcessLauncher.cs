using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Piscine.App.Launch;

/// <summary>Lance réellement via <see cref="Process"/> (UseShellExecute : ouvre dossiers/éditeurs via le shell).</summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    public bool Launch(LaunchSpec spec)
    {
        try
        {
            var psi = new ProcessStartInfo(spec.FileName) { UseShellExecute = true };
            foreach (var a in spec.Arguments)
            {
                psi.ArgumentList.Add(a);
            }
            return Process.Start(psi) is not null;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return false;
        }
    }
}
