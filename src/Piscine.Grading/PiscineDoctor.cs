using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Core.Content;

namespace Piscine.Grading;

public enum DoctorCheckStatus
{
    Ok,
    Warning,
    Error
}

public sealed record DoctorCheck(string Name, DoctorCheckStatus Status, string Detail);

public sealed class DoctorReport
{
    public DoctorReport(IReadOnlyList<DoctorCheck> checks) => Checks = checks;

    public IReadOnlyList<DoctorCheck> Checks { get; }

    public bool IsHealthy => Checks.All(check => check.Status != DoctorCheckStatus.Error);
}

/// <summary>Diagnostic local sans réseau du runtime, du contenu et de l'environnement d'exécution.</summary>
public static class PiscineDoctor
{
    public static DoctorReport Inspect(
        PiscineLayout layout,
        string? sandboxPath = null,
        System.Version? runtimeVersion = null,
        string? pathEnvironment = null)
    {
        var checks = new List<DoctorCheck>();
        InspectRuntime(runtimeVersion ?? Environment.Version, checks);
        InspectContent(layout, checks);
        InspectState(layout, checks);
        InspectSandbox(sandboxPath ?? SandboxLocator.Resolve(), checks);
        InspectWorkspace(layout, checks);
        InspectEditor(pathEnvironment ?? Environment.GetEnvironmentVariable("PATH"), checks);
        return new DoctorReport(checks);
    }

    private static void InspectRuntime(System.Version runtime, List<DoctorCheck> checks)
    {
        checks.Add(runtime.Major >= 10
            ? new DoctorCheck("Runtime .NET", DoctorCheckStatus.Ok, runtime.ToString())
            : new DoctorCheck("Runtime .NET", DoctorCheckStatus.Error, $".NET 10 requis, runtime détecté : {runtime}"));
    }

    private static void InspectContent(PiscineLayout layout, List<DoctorCheck> checks)
    {
        if (!Directory.Exists(layout.Content.ModulesDirectory))
        {
            checks.Add(new DoctorCheck("Contenu", DoctorCheckStatus.Error,
                $"dossier modules introuvable : {layout.Content.ModulesDirectory}"));
            return;
        }

        var count = ContentDiscovery.DiscoverModules(layout.Content).Count;
        checks.Add(count > 0
            ? new DoctorCheck("Contenu", DoctorCheckStatus.Ok, $"{count} module(s) chargé(s)")
            : new DoctorCheck("Contenu", DoctorCheckStatus.Error, "aucun module chargeable"));
    }

    private static void InspectState(PiscineLayout layout, List<DoctorCheck> checks)
    {
        var probe = Path.Combine(layout.StateDir, ".doctor-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            Directory.CreateDirectory(layout.StateDir);
            File.WriteAllText(probe, "ok");
            checks.Add(new DoctorCheck("État local", DoctorCheckStatus.Ok, $"accessible en écriture : {layout.StateDir}"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            checks.Add(new DoctorCheck("État local", DoctorCheckStatus.Error, e.Message));
        }
        finally
        {
            try
            {
                File.Delete(probe);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Le diagnostic principal a déjà été produit ; nettoyage best-effort.
            }
        }
    }

    private static void InspectSandbox(string? sandboxPath, List<DoctorCheck> checks)
    {
        checks.Add(sandboxPath is not null && File.Exists(sandboxPath)
            ? new DoctorCheck("Bac à sable", DoctorCheckStatus.Ok, sandboxPath)
            : new DoctorCheck("Bac à sable", DoctorCheckStatus.Error,
                "Piscine.Sandbox introuvable ; reconstruis la solution ou vérifie PISCINE_SANDBOX."));
    }

    private static void InspectWorkspace(PiscineLayout layout, List<DoctorCheck> checks)
    {
        if (!Directory.Exists(layout.WorkspaceRoot))
        {
            checks.Add(new DoctorCheck("Workspace", DoctorCheckStatus.Warning,
                $"non initialisé : lance piscine init ({layout.WorkspaceRoot})"));
            return;
        }

        if (!Repository.IsValid(layout.WorkspaceRoot))
        {
            checks.Add(new DoctorCheck("Git", DoctorCheckStatus.Warning,
                "le workspace existe mais n'est pas un dépôt Git ; relance piscine init."));
            return;
        }

        using var repository = new Repository(layout.WorkspaceRoot);
        if (!string.Equals(repository.Head.FriendlyName, "main", StringComparison.Ordinal))
        {
            checks.Add(new DoctorCheck("Branche Git", DoctorCheckStatus.Error,
                $"branche attendue : main ; branche actuelle : {repository.Head.FriendlyName}"));
        }
        else
        {
            checks.Add(new DoctorCheck("Branche Git", DoctorCheckStatus.Ok, "main"));
        }

        var origin = repository.Network.Remotes["origin"];
        checks.Add(origin is null
            ? new DoctorCheck("Remote Git", DoctorCheckStatus.Error, "remote origin absent")
            : new DoctorCheck("Remote Git", DoctorCheckStatus.Ok, origin.Url));
    }

    private static void InspectEditor(string? pathEnvironment, List<DoctorCheck> checks)
    {
        var editorOverride = Environment.GetEnvironmentVariable("PISCINE_EDITOR");
        if (!string.IsNullOrWhiteSpace(editorOverride))
        {
            checks.Add(new DoctorCheck("Éditeur", DoctorCheckStatus.Ok, $"PISCINE_EDITOR={editorOverride}"));
            return;
        }

        var detected = FindOnPath(pathEnvironment, OperatingSystem.IsWindows()
            ? new[] { "code.exe", "rider64.exe", "rider.exe", "devenv.exe" }
            : new[] { "code", "rider" });
        checks.Add(detected is null
            ? new DoctorCheck("Éditeur", DoctorCheckStatus.Warning,
                "aucun éditeur détecté dans PATH ; configure-le dans l'application si nécessaire.")
            : new DoctorCheck("Éditeur", DoctorCheckStatus.Ok, detected));
    }

    private static string? FindOnPath(string? pathEnvironment, IReadOnlyList<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(pathEnvironment))
        {
            return null;
        }

        foreach (var directory in pathEnvironment.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(directory.Trim(), candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}
