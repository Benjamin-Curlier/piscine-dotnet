using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Piscine.Grading;

/// <summary>Erreur recrue rapportée à travers la frontière du bac à sable (type + message).</summary>
public sealed record RunError(string TypeName, string Message);

/// <summary>Issue d'une exécution isolée d'un assembly compilé.</summary>
public sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, RunError? Error);

/// <summary>
/// Exécute un assembly console compilé en mémoire dans un contexte isolé
/// (<see cref="AssemblyLoadContext"/> collectible), en redirigeant stdin/stdout.
/// Partagé par <see cref="IoGrader"/> (correction) et <c>piscine try</c> (outillage auteur).
/// </summary>
public static class ProgramRunner
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static RunOutcome Run(byte[] assemblyBytes, string[] args, string stdin, TimeSpan? timeout = null)
    {
        var deadline = timeout ?? DefaultTimeout;
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
                return new RunOutcome(string.Empty, 0, false, new RunError(nameof(InvalidOperationException), "Aucun point d'entrée (Main)."));
            }

            Console.SetOut(output);
            Console.SetIn(new StringReader(stdin));

            int exitCode = 0;
            RunError? error = null;
            var task = Task.Run(() =>
            {
                try
                {
                    exitCode = InvokeEntry(entry, args);
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException ?? ex;
                    error = new RunError(inner.GetType().Name, inner.Message);
                }
                catch (Exception ex)
                {
                    error = new RunError(ex.GetType().Name, ex.Message);
                }
            });

            if (!task.Wait(deadline))
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
