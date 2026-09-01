using System;
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public sealed class PiscineDoctorTests
{
    [Fact]
    public void Inspect_ReadyUninitializedEnvironment_IsHealthyWithWorkspaceWarning()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"),
            "id: 00-setup\ntitle: Setup\norder: 0\ncourse: cours.md\ngroups: []\n");
        var sandbox = dir.WriteFile("Piscine.Sandbox.exe", string.Empty);
        var editor = dir.WriteFile(OperatingSystem.IsWindows() ? "code.exe" : "code", string.Empty);
        var layout = new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state"));

        var report = PiscineDoctor.Inspect(
            layout,
            sandboxPath: sandbox,
            runtimeVersion: new Version(10, 0, 4),
            pathEnvironment: Path.GetDirectoryName(editor));

        Assert.True(report.IsHealthy);
        Assert.Contains(report.Checks, check => check.Name == "Workspace" && check.Status == DoctorCheckStatus.Warning);
        Assert.Contains(report.Checks, check => check.Name == "Éditeur" && check.Status == DoctorCheckStatus.Ok);
    }

    [Fact]
    public void Inspect_OldRuntimeAndMissingContent_IsUnhealthy()
    {
        using var dir = new TempDir();
        var layout = new PiscineLayout(dir.Combine("content"), dir.Combine("workspace"), dir.Combine("state"));

        var report = PiscineDoctor.Inspect(layout, sandboxPath: dir.Combine("absent"), runtimeVersion: new Version(9, 0));

        Assert.False(report.IsHealthy);
        Assert.True(report.Checks.Count(check => check.Status == DoctorCheckStatus.Error) >= 3);
    }
}
