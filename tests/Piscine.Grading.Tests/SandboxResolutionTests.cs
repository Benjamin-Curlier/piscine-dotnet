using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// Garde de régression pour la résolution d'assemblies à travers la frontière de processus : le code
/// recrue référence souvent des assemblies présentes dans le processus de correction (Roslyn,
/// Microsoft.Extensions.*, Microsoft.Data.Sqlite…) mais absentes du jeu minimal du bac à sable. Le
/// bac à sable doit les résoudre via les ReferencePaths fournis par le parent. Sans cela, des
/// exercices entiers échouent en production alors que `dotnet test` reste vert (cf. validate-content).
/// </summary>
public class SandboxResolutionTests
{
    [Fact]
    public void Run_ResolvesHostReferencedAssembly_AcrossProcessBoundary()
    {
        // Microsoft.CodeAnalysis.CSharp est sur le TPA du processus de test (via Piscine.Grading)
        // mais n'est PAS une dépendance de Piscine.Sandbox : il doit être résolu via ReferencePaths.
        var bytes = CompilationService.Compile(
            new Dictionary<string, string>
            {
                ["P.cs"] = "System.Console.Write(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10);",
            },
            OutputKind.ConsoleApplication).AssemblyBytes;

        var run = ProgramRunner.Run(bytes, Array.Empty<string>(), stdin: "");

        Assert.False(run.TimedOut);
        Assert.Null(run.Error);
        Assert.Equal("CSharp10", run.Stdout);
    }
}
