using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Piscine.Sandbox;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxExecutorTests
{
    private static byte[] CompileXunit(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["Tests.cs"] = source },
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References).AssemblyBytes;

    [Fact]
    public void RunXunit_DisposesFixture_EvenWhenFactThrows()
    {
        var marker = Path.Combine(Path.GetTempPath(), $"sbx-dispose-{System.Guid.NewGuid():N}.txt");
        var markerLiteral = marker.Replace("\\", "\\\\");
        var source = $$"""
            using System;
            using System.IO;
            using Xunit;

            public class LeakyTests : IDisposable
            {
                [Fact]
                public void Boom() => throw new InvalidOperationException("boom");

                public void Dispose() => File.WriteAllText("{{markerLiteral}}", "disposed");
            }
            """;

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "xunit" }, CompileXunit(source));

        Assert.Equal(1, result.FactCount);
        Assert.Single(result.Failures);
        Assert.True(File.Exists(marker), "La fixture IDisposable n'a pas été disposée.");
        Assert.Equal("disposed", File.ReadAllText(marker));
        File.Delete(marker);
    }

    [Fact]
    public void RunXunit_ReportsPassAndFail()
    {
        var source = """
            using Xunit;
            public class T
            {
                [Fact] public void Ok() => Assert.True(true);
                [Fact] public void Ko() => Assert.Equal(1, 2);
            }
            """;

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "xunit" }, CompileXunit(source));

        Assert.Equal(2, result.FactCount);
        Assert.Single(result.Failures);
        Assert.Contains("Ko", result.Failures[0]);
    }
}
