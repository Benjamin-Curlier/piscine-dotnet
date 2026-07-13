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
    /// <summary>
    /// Plafond d'accumulation du stdout enfant (~16 M caractères, soit ~32 Mo en mémoire UTF-16). La
    /// moulinette est de confiance : une recrue qui inonde stdout ferait exploser la mémoire du parent
    /// AVANT le timeout. Au-delà du plafond on tue l'arbre et on ferme en « SortieTropVolumineuse »
    /// (jamais un faux « Réussi »). La trame verdict io légitime (sortie recrue capturée + wrapper JSON)
    /// reste très en deçà.
    /// </summary>
    private const int MaxStdoutChars = 16 * 1024 * 1024;

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
                // Lecture BORNÉE du stdout enfant par blocs de taille fixe — et NON via l'API orientée
                // ligne BeginOutputReadLine : la trame io est une SEULE ligne (potentiellement énorme)
                // que le lecteur du framework matérialiserait intégralement en mémoire AVANT tout
                // contrôle. Au-delà du plafond : kill de l'arbre + fail-closed « SortieTropVolumineuse »
                // (jamais un faux « Réussi »). Le verdict reste dérivé de la trame par ParseVerdict
                // (intégrité B-2). stderr est drainé et ignoré (drainage concurrent obligatoire : sinon
                // l'enfant peut se bloquer en remplissant le tampon stderr).
                var stdout = new StringBuilder();
                var overflowed = false;

                var stdoutTask = Task.Run(() =>
                {
                    try
                    {
                        var buffer = new char[64 * 1024];
                        int read;
                        while ((read = process.StandardOutput.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Couper AVANT d'accumuler : borne stricte la mémoire du parent au plafond.
                            if ((long)stdout.Length + read > MaxStdoutChars)
                            {
                                overflowed = true;
                                try { process.Kill(entireProcessTree: true); }
                                catch { /* déjà mort ou en cours d'arrêt */ }
                                break;
                            }

                            stdout.Append(buffer, 0, read);
                        }
                    }
                    catch { /* flux fermé (kill/timeout/dispose) : fin de lecture */ }
                });

                var stderrTask = Task.Run(() =>
                {
                    try
                    {
                        var buffer = new char[16 * 1024];
                        while (process.StandardError.Read(buffer, 0, buffer.Length) > 0)
                        {
                            // ignoré, mais drainé pour ne pas bloquer l'enfant
                        }
                    }
                    catch { /* flux fermé : fin de lecture */ }
                });

                // Clamp : TotalMilliseconds d'un très grand TimeSpan déborderait le cast int en négatif
                // (WaitForExit(-n) = attente infinie). Les timeouts réels valent quelques secondes.
                var waitMs = timeout.TotalMilliseconds >= int.MaxValue ? int.MaxValue : (int)timeout.TotalMilliseconds;
                if (!process.WaitForExit(waitMs))
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch { /* déjà mort */ }
                    process.WaitForExit(2000); // borne courte : ne pas bloquer si un descendant survit
                    try { Task.WaitAll(new[] { stdoutTask, stderrTask }, 2000); }
                    catch { /* lectures annulées/terminées */ }
                    timedOut = true;
                    return new SandboxResult();
                }

                process.WaitForExit(); // s'assurer que le processus est pleinement terminé

                // Joindre les lecteurs (borne courte) : établit un happens-before pour lire stdout/overflowed.
                try { Task.WaitAll(new[] { stdoutTask, stderrTask }, 2000); }
                catch { /* lectures annulées/terminées */ }

                if (overflowed)
                {
                    // Fail-closed : sortie ingérable ⇒ jamais un « Réussi », on signale l'anomalie.
                    return new SandboxResult
                    {
                        ErrorType = "SortieTropVolumineuse",
                        ErrorMessage = "Le bac à sable a produit trop de sortie (borne dépassée).",
                    };
                }

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
