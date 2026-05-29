using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class CompilationServiceTests
{
    [Fact]
    public void Compile_Succeeds_OnValidConsoleProgram()
    {
        var sources = new Dictionary<string, string>
        {
            ["Hello.cs"] = """
                System.Console.WriteLine("Hi");
                """
        };

        var result = CompilationService.Compile(sources, OutputKind.ConsoleApplication);

        Assert.True(result.Success);
        Assert.NotEmpty(result.AssemblyBytes);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Compile_Fails_AndReportsErrors_OnSyntaxError()
    {
        var sources = new Dictionary<string, string>
        {
            ["Bad.cs"] = "this is not valid C#"
        };

        var result = CompilationService.Compile(sources, OutputKind.ConsoleApplication);

        Assert.False(result.Success);
        Assert.Empty(result.AssemblyBytes);
        Assert.NotEmpty(result.Errors);
    }
}
