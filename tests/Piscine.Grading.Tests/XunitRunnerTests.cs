using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class XunitRunnerTests
{
    private static byte[] Compile(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["Tests.cs"] = source },
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References).AssemblyBytes;

    [Fact]
    public void Run_CountsFactsAndFailures()
    {
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                [Fact] public void A() => Assert.True(true);
                [Fact] public void B() => Assert.Equal(1, 2);
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.Equal(2, run.FactCount);
        Assert.Single(run.Failures);
    }

    [Fact]
    public void Run_InfiniteLoopInFact_TimesOut()
    {
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                [Fact] public void Loop() { while (true) { System.Threading.Thread.Sleep(1); } }
            }
            """), TimeSpan.FromMilliseconds(500));

        Assert.True(run.TimedOut);
    }

    [Fact]
    public void Run_DisposesFixture_EndToEnd()
    {
        var marker = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sbx-e2e-{Guid.NewGuid():N}.txt");
        var lit = marker.Replace("\\", "\\\\");
        var run = XunitRunner.Run(Compile($$"""
            using System;
            using System.IO;
            using Xunit;
            public class T : IDisposable
            {
                [Fact] public void A() => Assert.True(true);
                public void Dispose() => File.WriteAllText("{{lit}}", "ok");
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.True(System.IO.File.Exists(marker), "Fixture non disposée à travers le processus enfant.");
        System.IO.File.Delete(marker);
    }
}
