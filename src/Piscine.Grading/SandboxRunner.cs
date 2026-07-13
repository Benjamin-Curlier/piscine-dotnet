using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
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
                // Accumuler stdout (drainage concurrent obligatoire : la trame io embarque la sortie
                // recrue et peut être volumineuse) ; ignorer stderr. Le verdict est dérivé de la trame
                // par ParseVerdict — jamais d'un fichier écrit dans workDir (intégrité B-2).
                var stdout = new StringBuilder();
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data is not null)
                    {
                        stdout.Append(e.Data).Append('\n');
                    }
                };
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

                return ParseVerdict(stdout.ToString(), process.ExitCode);
            }
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch { /* nettoyage best-effort */ }
        }
    }

    /// <summary>
    /// Dérive le résultat autoritaire depuis la sortie standard de l'enfant. Condition d'acceptation
    /// centrée sur la TRAME, pas sur le code de sortie (un programme io légitime peut sortir non-nul
    /// via Environment.Exit) : exactement une occurrence de la sentinelle dont le JSON se désérialise
    /// ⇒ acceptée ; zéro (FailFast, StackOverflow… : aucune trame), ≥2 (trame injectée par la recrue)
    /// ou JSON illisible ⇒ fail-closed « ArrêtAnormal ».
    /// </summary>
    private static SandboxResult ParseVerdict(string stdout, int exitCode)
    {
        var sentinel = SandboxProtocol.VerdictSentinel;
        var first = stdout.IndexOf(sentinel, StringComparison.Ordinal);
        var last = stdout.LastIndexOf(sentinel, StringComparison.Ordinal);
        if (first >= 0 && first == last)
        {
            var start = first + sentinel.Length;
            var newline = stdout.IndexOf('\n', start);
            var json = newline >= 0 ? stdout[start..newline] : stdout[start..];
            try
            {
                var result = JsonSerializer.Deserialize(json, SandboxJsonContext.Default.SandboxResult);
                if (result is not null)
                {
                    // Sortie anticipée (Environment.Exit) : le vrai code de sortie vit dans le processus
                    // (la trame est émise avant la fin) ⇒ on l'y recolle.
                    if (result.ExitedEarly)
                    {
                        result.ExitCode = exitCode;
                    }

                    return result;
                }
            }
            catch (JsonException)
            {
                // Trame illisible ⇒ on tombe en fail-closed ci-dessous.
            }
        }

        return new SandboxResult
        {
            ErrorType = "ArrêtAnormal",
            ErrorMessage = first >= 0 && first != last
                ? $"Le bac à sable a produit plusieurs verdicts (sortie falsifiée, code {exitCode})."
                : $"Le bac à sable s'est arrêté anormalement (code {exitCode}) sans produire de verdict.",
        };
    }
}
