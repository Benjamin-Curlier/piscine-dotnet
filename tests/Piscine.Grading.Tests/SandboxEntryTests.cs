using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Piscine.Sandbox;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxEntryTests
{
    [Fact]
    public void Run_ExecutesIo_AndWritesResultJson()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"sbx-entry-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        try
        {
            var bytes = CompilationService.Compile(
                new Dictionary<string, string> { ["P.cs"] = "System.Console.Write(\"OK\"); return 0;" },
                OutputKind.ConsoleApplication).AssemblyBytes;
            File.WriteAllBytes(Path.Combine(workDir, "asm.dll"), bytes);
            File.WriteAllText(
                Path.Combine(workDir, "request.json"),
                JsonSerializer.Serialize(new SandboxRequest { Mode = "io" }, SandboxJsonContext.Default.SandboxRequest));

            var code = SandboxEntry.Run(workDir);

            Assert.Equal(0, code);
            var resultPath = Path.Combine(workDir, "result.json");
            Assert.True(File.Exists(resultPath));
            var result = JsonSerializer.Deserialize(File.ReadAllText(resultPath), SandboxJsonContext.Default.SandboxResult)!;
            Assert.Equal("OK", result.Stdout);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }
}
