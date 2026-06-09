using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Piscine.Sandbox;

/// <summary>
/// Cœur d'exécution du code non fiable : charge l'assembly recrue dans un ALC et exécute,
/// soit le point d'entrée (io), soit les méthodes [Fact] (xunit). Appelable en proc (tests).
/// </summary>
public static class SandboxExecutor
{
    public static SandboxResult Execute(SandboxRequest request, byte[] assemblyBytes)
    {
        var alc = new AssemblyLoadContext("submission", isCollectible: true);
        alc.Resolving += (ctx, name) =>
        {
            foreach (var path in request.ReferencePaths)
            {
                if (string.Equals(
                        System.IO.Path.GetFileNameWithoutExtension(path),
                        name.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ctx.LoadFromAssemblyPath(path);
                }
            }

            return null;
        };

        using var ms = new System.IO.MemoryStream(assemblyBytes);
        var assembly = alc.LoadFromStream(ms);

        return request.Mode switch
        {
            "io" => RunIo(assembly, request.Args, request.Stdin),
            "xunit" => RunXunit(assembly),
            _ => new SandboxResult { ErrorType = "ArgumentException", ErrorMessage = $"Mode inconnu : {request.Mode}" },
        };
    }

    private static SandboxResult RunIo(Assembly assembly, string[] args, string stdin)
    {
        var entry = assembly.EntryPoint;
        if (entry is null)
        {
            return new SandboxResult
            {
                ErrorType = nameof(InvalidOperationException),
                ErrorMessage = "Aucun point d'entrée (Main).",
            };
        }

        var output = new System.IO.StringWriter();
        var originalOut = Console.Out;
        var originalIn = Console.In;
        Console.SetOut(output);
        Console.SetIn(new System.IO.StringReader(stdin));
        try
        {
            int exitCode = InvokeEntry(entry, args);
            return new SandboxResult { Stdout = output.ToString(), ExitCode = exitCode };
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException ?? ex;
            return new SandboxResult { Stdout = output.ToString(), ErrorType = inner.GetType().Name, ErrorMessage = inner.Message };
        }
        catch (Exception ex)
        {
            return new SandboxResult { Stdout = output.ToString(), ErrorType = ex.GetType().Name, ErrorMessage = ex.Message };
        }
        finally
        {
            // Restauré car RunIo est aussi appelé en proc dans les tests (exécution synchrone,
            // aucune tâche orpheline ne survit à ce point ⇒ restauration sûre). Dans le processus
            // enfant, la restauration est inoffensive (un seul run puis sortie).
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
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
            _ => 0,
        };
    }

    private static int Await(Task task)
    {
        task.GetAwaiter().GetResult();
        return 0;
    }

    private static SandboxResult RunXunit(Assembly assembly)
    {
        var methods = FindFactMethods(assembly);
        var failures = new List<string>();
        foreach (var method in methods)
        {
            var error = RunOne(method);
            if (error is not null)
            {
                failures.Add($"{method.DeclaringType?.Name}.{method.Name} : {error}");
            }
        }

        return new SandboxResult { FactCount = methods.Count, Failures = failures.ToArray() };
    }

    private static List<MethodInfo> FindFactMethods(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetParameters().Length == 0
                && m.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "Xunit.FactAttribute"))
            .ToList();

    private static string? RunOne(MethodInfo method)
    {
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(method.DeclaringType!);
            var result = method.Invoke(instance, Array.Empty<object>());
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
            }

            return null;
        }
        catch (TargetInvocationException ex)
        {
            return (ex.InnerException ?? ex).Message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            // Hygiène : disposer la fixture (recrues tenant fichiers/sockets fuyaient avant).
            if (instance is IDisposable d)
            {
                d.Dispose();
            }
            else if (instance is IAsyncDisposable ad)
            {
                ad.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
    }
}
