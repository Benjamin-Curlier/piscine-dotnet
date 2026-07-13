using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

/// <summary>
/// Vérifie que le vecteur d'intégrité documenté (B-2) est neutralisé. Le code recrue s'exécute dans
/// l'enfant NON fiable, mais il n'a plus aucun fichier de résultat à falsifier : le verdict
/// autoritaire est dérivé par le parent depuis la trame stdout, jamais depuis un fichier. Une recrue
/// qui écrit un result.json contrefait (passant) dans le workDir découvert via argv, puis FailFast
/// (pour sauter le flush), doit obtenir un ArrêtAnormal — et non un faux succès.
/// </summary>
public class SandboxEntryTests
{
    [Fact]
    public void ForgedResultFileInWorkDir_ThenFailFast_IsNotTrustedAsPass()
    {
        var forger = CompileXunit("""
            using System;
            using System.IO;
            using Xunit;

            public class Forge
            {
                [Fact]
                public void Cheat()
                {
                    // Vecteur documenté : écrire un result.json passant dans le workDir découvert via
                    // argv, puis FailFast (saute le flush ProcessExit). Le parent ne lit plus ce
                    // fichier ; aucune trame n'est émise ⇒ fail-closed.
                    var argv = Environment.GetCommandLineArgs();
                    if (argv.Length > 1)
                    {
                        File.WriteAllText(
                            Path.Combine(argv[1], "result.json"),
                            "{\"FactCount\":5,\"Failures\":[]}");
                    }

                    Environment.FailFast("forge");
                }
            }
            """);

        var run = XunitRunner.Run(forger, TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.False(run.FactCount == 5 && run.Failures.Count == 0); // la contrefaçon est ignorée
        Assert.Equal(0, run.FactCount);                              // fail-closed : aucune trame
    }

    private static byte[] CompileXunit(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["Forge.cs"] = source },
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References).AssemblyBytes;
}
