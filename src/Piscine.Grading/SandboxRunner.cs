using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>Le binaire du bac à sable est introuvable ou ne peut être lancé (erreur interne, fail-closed).</summary>
public sealed class SandboxUnavailableException : Exception
{
    public SandboxUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Prépare le lancement du bac à sable depuis le chemin résolu par <see cref="SandboxLocator"/>.</summary>
internal static class SandboxLauncher
{
    public static ProcessStartInfo CreateStartInfo(string workDir)
    {
        var path = SandboxLocator.Resolve()
            ?? throw new SandboxUnavailableException(
                "Binaire du bac à sable introuvable (ni PISCINE_SANDBOX, ni co-localisé, ni sous-dossier sandbox/, ni build dev).");

        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // Cas normal : un apphost (Piscine.Sandbox(.exe), self-contained en prod / framework-dependent
        // en dev+tests) lancé directement. Une surcharge .dll passe par le muxer dotnet.
        if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            psi.FileName = DotnetMuxer();
            psi.ArgumentList.Add(path);
        }
        else
        {
            psi.FileName = path;
        }

        psi.ArgumentList.Add(workDir);
        return psi;
    }

    private static string DotnetMuxer()
    {
        var current = Environment.ProcessPath;
        if (current is not null
            && Path.GetFileNameWithoutExtension(current).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(root))
        {
            var candidate = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
    }
}

/// <summary>Lance le bac à sable dans un processus enfant jetable et renvoie son résultat.</summary>
internal static class SandboxProcess
{
    public static SandboxResult Run(SandboxRequest request, byte[] assemblyBytes, TimeSpan timeout, out bool timedOut)
    {
        timedOut = false;
        var workDir = Path.Combine(Path.GetTempPath(), "piscine-sbx", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            File.WriteAllBytes(Path.Combine(workDir, "asm.dll"), assemblyBytes);
            File.WriteAllText(
                Path.Combine(workDir, "request.json"),
                JsonSerializer.Serialize(request, SandboxJsonContext.Default.SandboxRequest));

            var psi = SandboxLauncher.CreateStartInfo(workDir);
            Process process;
            try
            {
                process = Process.Start(psi)
                    ?? throw new SandboxUnavailableException("Process.Start a renvoyé null pour le bac à sable.");
            }
            catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
            {
                throw new SandboxUnavailableException($"Lancement du bac à sable impossible : {ex.Message}", ex);
            }

            using (process)
            {
                // Drainer stdout/stderr pour éviter un blocage de pipe (le protocole passe par fichier).
                process.OutputDataReceived += static (_, _) => { };
                process.ErrorDataReceived += static (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Clamp : TotalMilliseconds d'un très grand TimeSpan déborderait le cast int en négatif
                // (WaitForExit(-n) = attente infinie). Les timeouts réels valent quelques secondes.
                var waitMs = timeout.TotalMilliseconds >= int.MaxValue ? int.MaxValue : (int)timeout.TotalMilliseconds;
                if (!process.WaitForExit(waitMs))
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch { /* déjà mort */ }
                    process.WaitForExit();
                    timedOut = true;
                    return new SandboxResult();
                }

                process.WaitForExit(); // s'assurer que les handlers async ont vidé

                var resultPath = Path.Combine(workDir, "result.json");
                if (File.Exists(resultPath))
                {
                    var result = JsonSerializer.Deserialize(
                        File.ReadAllText(resultPath), SandboxJsonContext.Default.SandboxResult);
                    if (result is not null)
                    {
                        if (result.ExitedEarly)
                        {
                            // Sortie anticipée légitime (Environment.Exit(n) en io) : le code programme
                            // fait foi.
                            result.ExitCode = process.ExitCode;
                        }
                        else if (process.ExitCode != 0)
                        {
                            // Le chemin légitime (SandboxEntry.Run) renvoie 0 hors sortie anticipée. Un
                            // result.json présent AVEC un code de sortie non nul = crash après écriture
                            // partielle ou terminaison forcée : résultat non fiable → fail-closed.
                            return new SandboxResult
                            {
                                ErrorType = "ArrêtAnormal",
                                ErrorMessage = $"Le bac à sable s'est terminé anormalement (code {process.ExitCode}) après avoir produit un résultat.",
                            };
                        }

                        return result;
                    }
                }

                // Pas de result.json et pas de timeout ⇒ arrêt anormal (StackOverflow, FailFast…).
                return new SandboxResult
                {
                    ErrorType = "ArrêtAnormal",
                    ErrorMessage = $"Le bac à sable s'est arrêté anormalement (code {process.ExitCode}) sans produire de résultat.",
                };
            }
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch { /* nettoyage best-effort */ }
        }
    }
}
