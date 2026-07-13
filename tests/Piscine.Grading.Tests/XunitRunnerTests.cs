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
    public void Run_CountsTheoryInlineDataRows_AsSeparateCases()
    {
        // M-6 : chaque ligne [InlineData] compte comme un cas (2 verts + 1 rouge ici).
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                [Theory]
                [InlineData(2)]
                [InlineData(4)]
                [InlineData(3)]
                public void Pair(int n) => Assert.True(n % 2 == 0);
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.False(run.Crashed);
        Assert.Equal(3, run.FactCount);
        Assert.Single(run.Failures); // le cas n=3 échoue
    }

    [Fact]
    public void Run_TheoryWithoutInlineData_IsReportedNotSilentlySkipped()
    {
        // M-6 fail-closed : une source de données non supportée (MemberData) ne doit pas être ignorée.
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            using System.Collections.Generic;
            public class T
            {
                public static IEnumerable<object[]> Data => new[] { new object[] { 1 } };
                [Theory]
                [MemberData(nameof(Data))]
                public void M(int n) => Assert.True(true);
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.Equal(1, run.FactCount);
        Assert.Single(run.Failures); // signalée comme échec, pas sautée
    }

    [Fact]
    public void Run_StackOverflowInFact_IsFlaggedCrashed_NotSilentPass()
    {
        // M-5 amont : un arrêt anormal (StackOverflow) est signalé Crashed, pas un run vert vide.
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                static int Boom(int n) => Boom(n + 1);
                [Fact] public void A() => Boom(0);
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.RanCleanly); // ni vert exploitable : TimedOut ou Crashed
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
