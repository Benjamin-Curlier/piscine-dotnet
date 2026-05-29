using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Grader <c>io</c> : compile le programme de la recrue, l'exécute dans un contexte isolé,
/// et compare la sortie standard / le code de sortie aux attentes.
/// </summary>
public sealed class IoGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public string Type => "io";

    public GraderResult Grade(IReadOnlyDictionary<string, string> sources, GradingStep step)
    {
        var compilation = CompilationService.Compile(sources, OutputKind.ConsoleApplication);
        if (!compilation.Success)
        {
            var messages = new List<string> { "Le programme ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray());
        }

        foreach (var ioCase in step.Cases)
        {
            var run = Execute(compilation.AssemblyBytes, ioCase.Args.ToArray(), ioCase.Stdin);

            if (run.TimedOut)
            {
                return GraderResult.Failure(Type, "Votre programme ne s'est pas terminé à temps (boucle infinie ?).");
            }

            if (run.Error is not null)
            {
                return GraderResult.Failure(Type, $"Votre programme a levé une exception : {run.Error.GetType().Name} — {run.Error.Message}");
            }

            if (Normalize(run.Stdout) != Normalize(ioCase.ExpectStdout))
            {
                return GraderResult.Failure(
                    Type,
                    "La sortie ne correspond pas.",
                    $"Attendu : {Quote(ioCase.ExpectStdout)}",
                    $"Obtenu  : {Quote(run.Stdout)}");
            }

            if (run.ExitCode != ioCase.ExpectExit)
            {
                return GraderResult.Failure(
                    Type,
                    $"Code de sortie inattendu : attendu {ioCase.ExpectExit}, obtenu {run.ExitCode}.");
            }
        }

        return GraderResult.Success(Type);
    }

    private static string Normalize(string s) => s.Replace("\r\n", "\n");

    private static string Quote(string s) => "\"" + s.Replace("\n", "\\n") + "\"";

    private sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, Exception? Error);

    private static RunOutcome Execute(byte[] assemblyBytes, string[] args, string stdin)
    {
        var alc = new AssemblyLoadContext("submission", isCollectible: true);
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var output = new StringWriter();
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var entry = assembly.EntryPoint;
            if (entry is null)
            {
                return new RunOutcome(string.Empty, 0, false, new InvalidOperationException("Aucun point d'entrée (Main)."));
            }

            Console.SetOut(output);
            Console.SetIn(new StringReader(stdin));

            int exitCode = 0;
            Exception? error = null;
            var task = Task.Run(() =>
            {
                try
                {
                    exitCode = InvokeEntry(entry, args);
                }
                catch (TargetInvocationException ex)
                {
                    error = ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });

            if (!task.Wait(Timeout))
            {
                return new RunOutcome(output.ToString(), 0, true, null);
            }

            return new RunOutcome(output.ToString(), exitCode, false, error);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
            alc.Unload();
        }
    }

    private static int InvokeEntry(MethodInfo entry, string[] args)
    {
        var invokeArgs = entry.GetParameters().Length == 1
            ? new object[] { args }
            : Array.Empty<object>();

        var result = entry.Invoke(null, invokeArgs);
        return result switch
        {
            int code => code,
            Task<int> taskInt => taskInt.GetAwaiter().GetResult(),
            Task task => Await(task),
            _ => 0
        };
    }

    private static int Await(Task task)
    {
        task.GetAwaiter().GetResult();
        return 0;
    }
}
