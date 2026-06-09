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

/// <summary>Résout la commande à lancer pour le bac à sable (surcharge env, apphost, ou dotnet+dll).</summary>
internal static class SandboxLauncher
{
    public static ProcessStartInfo CreateStartInfo(string workDir)
    {
        var (file, prefixArgs) = ResolveCommand();
        var psi = new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in prefixArgs)
        {
            psi.ArgumentList.Add(a);
        }

        psi.ArgumentList.Add(workDir);
        return psi;
    }

    private static (string File, string[] PrefixArgs) ResolveCommand()
    {
        var overridePath = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        if (!string.IsNullOrEmpty(overridePath))
        {
            // Surcharge autoritaire : pas de repli. Un .dll ⇒ via le muxer dotnet ; sinon apphost direct.
            return overridePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? (DotnetMuxer(), new[] { overridePath })
                : (overridePath, Array.Empty<string>());
        }

        var baseDir = AppContext.BaseDirectory;
        var exeName = OperatingSystem.IsWindows() ? "Piscine.Sandbox.exe" : "Piscine.Sandbox";
        var apphost = Path.Combine(baseDir, exeName);
        if (File.Exists(apphost))
        {
            return (apphost, Array.Empty<string>());
        }

        var dll = Path.Combine(baseDir, "Piscine.Sandbox.dll");
        if (File.Exists(dll))
        {
            return (DotnetMuxer(), new[] { dll });
        }

        throw new SandboxUnavailableException(
            $"Binaire du bac à sable introuvable près de « {baseDir} » (ni {exeName} ni Piscine.Sandbox.dll).");
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

                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
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
                            result.ExitCode = process.ExitCode;
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
