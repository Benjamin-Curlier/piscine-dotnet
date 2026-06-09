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

    private static byte[] CompileIo(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["P.cs"] = source },
            OutputKind.ConsoleApplication).AssemblyBytes;

    [Fact]
    public void RunIo_CapturesStdout_AndExitCode()
    {
        var bytes = CompileIo("""
            System.Console.Write("Hello");
            return 7;
            """);

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io" }, bytes);

        Assert.Equal("Hello", result.Stdout);
        Assert.Equal(7, result.ExitCode);
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void RunIo_ReportsUncaughtException()
    {
        var bytes = CompileIo("""throw new System.InvalidOperationException("nope");""");

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io" }, bytes);

        Assert.Equal("InvalidOperationException", result.ErrorType);
        Assert.Equal("nope", result.ErrorMessage);
    }

    [Fact]
    public void RunIo_FeedsStdin()
    {
        var bytes = CompileIo("""System.Console.Write(System.Console.ReadLine());""");

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io", Stdin = "écho" }, bytes);

        Assert.Equal("écho", result.Stdout);
    }
}
