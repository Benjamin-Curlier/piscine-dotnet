using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;

// Shim « git » : intercepte git en tete de PATH, relaie de facon TRANSPARENTE vers le vrai git,
// puis emet (fire-and-forget) un evenement structure {argv, exitCode, cwd} sur un named pipe local.
// Le shim ne modifie JAMAIS le comportement de git : pas de parsing de stdout, code de sortie relaye
// tel quel, et toute panne du canal de coaching est avalee silencieusement.

var realGit = ResolveRealGit();
if (realGit is null)
{
    await Console.Error.WriteLineAsync(
        "piscine: vrai git introuvable (ni PISCINE_REAL_GIT, ni dans PATH hors dossier du shim).");
    return 127;
}

// Relais transparent : on herite stdin/stdout/stderr, on attend la fin, on capture le code.
var psi = new ProcessStartInfo(realGit) { UseShellExecute = false };
foreach (var arg in args)
{
    psi.ArgumentList.Add(arg);
}

using var proc = new Process { StartInfo = psi };
proc.Start();
await proc.WaitForExitAsync();
var exitCode = proc.ExitCode;

// Emission fire-and-forget : ne bloque jamais git, meme si personne n'ecoute.
await TryEmitAsync(args, exitCode);

return exitCode;

static string? ResolveRealGit()
{
    // 1) PISCINE_REAL_GIT s'il pointe sur un fichier existant (chemin absolu fixe par PtyService).
    var declared = Environment.GetEnvironmentVariable("PISCINE_REAL_GIT");
    if (!string.IsNullOrEmpty(declared) && File.Exists(declared))
    {
        return declared;
    }

    // 2) Defense secondaire : recherche PATH en s'excluant soi-meme (jamais de recursion shim->shim).
    var shimDir = Path.GetDirectoryName(Environment.ProcessPath);
    var pathVar = Environment.GetEnvironmentVariable("PATH");
    if (string.IsNullOrEmpty(pathVar))
    {
        return null;
    }

    var candidates = OperatingSystem.IsWindows()
        ? new[] { "git.exe", "git.cmd", "git" }
        : new[] { "git" };

    foreach (var dir in pathVar.Split(Path.PathSeparator))
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            continue;
        }

        string fullDir;
        try
        {
            fullDir = Path.GetFullPath(dir);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            continue;
        }

        if (shimDir is not null
            && string.Equals(fullDir.TrimEnd(Path.DirectorySeparatorChar),
                shimDir.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        foreach (var name in candidates)
        {
            var candidate = Path.Combine(fullDir, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    return null;
}

static async Task TryEmitAsync(string[] argv, int exitCode)
{
    var pipeName = Environment.GetEnvironmentVariable("PISCINE_COACH_PIPE");
    if (string.IsNullOrEmpty(pipeName))
    {
        return;
    }

    try
    {
        await using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        client.Connect(150);

        var payload = new ShimPayload(argv, exitCode, Directory.GetCurrentDirectory());
        var json = JsonSerializer.Serialize(payload, ShimJsonContext.Default.ShimPayload);

        await using var writer = new StreamWriter(client) { AutoFlush = true };
        await writer.WriteLineAsync(json);
    }
    catch (Exception e) when (e is TimeoutException or IOException or UnauthorizedAccessException)
    {
        // Canal de coaching indisponible (app fermee, course, charge) : abandon silencieux.
        // git n'est JAMAIS altere — le code de sortie reel a deja ete capture.
    }
}

internal sealed record ShimPayload(
    [property: JsonPropertyName("argv")] IReadOnlyList<string> Argv,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("cwd")] string Cwd);

[JsonSerializable(typeof(ShimPayload))]
internal sealed partial class ShimJsonContext : JsonSerializerContext;
