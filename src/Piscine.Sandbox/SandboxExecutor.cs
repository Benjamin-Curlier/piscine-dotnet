using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        // Résolution managée : les dépendances du code recrue (Microsoft.Extensions.*,
        // Microsoft.Data.Sqlite, …) absentes du jeu minimal du processus enfant sont chargées depuis
        // les chemins runtime du processus de correction.
        alc.Resolving += (ctx, name) =>
        {
            foreach (var path in request.ReferencePaths)
            {
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        name.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ctx.LoadFromAssemblyPath(path);
                }
            }

            return null;
        };

        // Résolution native : ex. e_sqlite3 pour Microsoft.Data.Sqlite. On sonde les dossiers des
        // assemblies de référence et leurs sous-dossiers runtimes/<rid>/native.
        alc.ResolvingUnmanagedDll += (_, libName) => ResolveNativeLibrary(libName, request.ReferencePaths);

        using var ms = new MemoryStream(assemblyBytes);
        var assembly = alc.LoadFromStream(ms);

        return request.Mode switch
        {
            "io" => RunIo(assembly, request.Args, request.Stdin),
            "xunit" => RunXunit(assembly),
            _ => new SandboxResult { ErrorType = "ArgumentException", ErrorMessage = $"Mode inconnu : {request.Mode}" },
        };
    }

    private static IntPtr ResolveNativeLibrary(string libName, string[] referencePaths)
    {
        var dirs = referencePaths
            .Select(Path.GetDirectoryName)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rids = NativeRids();
        var fileNames = NativeFileNames(libName);

        foreach (var dir in dirs)
        {
            foreach (var file in fileNames)
            {
                var direct = Path.Combine(dir!, file);
                if (File.Exists(direct) && NativeLibrary.TryLoad(direct, out var h1))
                {
                    return h1;
                }

                foreach (var rid in rids)
                {
                    var nativePath = Path.Combine(dir!, "runtimes", rid, "native", file);
                    if (File.Exists(nativePath) && NativeLibrary.TryLoad(nativePath, out var h2))
                    {
                        return h2;
                    }
                }
            }
        }

        return IntPtr.Zero; // laisser la résolution par défaut tenter sa chance
    }

    private static string[] NativeRids()
    {
        var arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(); // x64, arm64, x86…
        var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        var portable = $"{os}-{arch}";
        var current = RuntimeInformation.RuntimeIdentifier;
        return current == portable ? new[] { current } : new[] { current, portable };
    }

    private static string[] NativeFileNames(string libName)
    {
        if (OperatingSystem.IsWindows())
        {
            return new[] { libName + ".dll", libName };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new[] { "lib" + libName + ".dylib", libName + ".dylib", libName };
        }

        return new[] { "lib" + libName + ".so", libName + ".so", libName };
    }

    /// <summary>
    /// Writer de capture du stdout io en cours. Exposé pour que le point d'entrée puisse récupérer la
    /// sortie déjà produite si la recrue court-circuite le retour via <c>Environment.Exit</c> (M-4) —
    /// <c>Console.Out</c> renvoie un wrapper synchronisé, pas directement ce StringWriter.
    /// </summary>
    internal static System.IO.StringWriter? CurrentIoCapture { get; private set; }

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
        CurrentIoCapture = output;
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
            // enfant, la restauration est inoffensive (un seul run puis sortie). Ce finally NE tourne
            // PAS si la recrue appelle Environment.Exit : CurrentIoCapture reste alors dispo pour le
            // point d'entrée (M-4). En retour normal, on le nettoie (result déjà renvoyé).
            CurrentIoCapture = null;
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

    private const string FactAttributeName = "Xunit.FactAttribute";
    private const string TheoryAttributeName = "Xunit.TheoryAttribute";
    private const string InlineDataAttributeName = "Xunit.InlineDataAttribute";

    /// <summary>Un cas de test exécutable (une méthode + ses arguments). <c>Unsupported</c> non nul = échec porté.</summary>
    private sealed record TestCase(MethodInfo Method, object?[] Args, string Display, string? Unsupported);

    private static SandboxResult RunXunit(Assembly assembly)
    {
        var cases = FindTestCases(assembly);
        var failures = new List<string>();
        foreach (var testCase in cases)
        {
            var error = testCase.Unsupported ?? RunOne(testCase.Method, testCase.Args);
            if (error is not null)
            {
                failures.Add($"{testCase.Display} : {error}");
            }
        }

        return new SandboxResult { FactCount = cases.Count, Failures = failures.ToArray() };
    }

    /// <summary>
    /// Découvre les cas de test : chaque <c>[Fact]</c> (sans paramètre) donne un cas, chaque
    /// <c>[Theory]</c> un cas par ligne <c>[InlineData]</c>. Une <c>[Theory]</c> dont les données
    /// viennent d'une autre source (MemberData/ClassData) n'est PAS sautée silencieusement (fausserait
    /// la notation, M-6) : elle est portée comme un échec explicite (fail-closed).
    /// </summary>
    private static List<TestCase> FindTestCases(Assembly assembly)
    {
        var cases = new List<TestCase>();
        var methods = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance));

        foreach (var method in methods)
        {
            var attrs = method.GetCustomAttributesData();
            var name = $"{method.DeclaringType?.Name}.{method.Name}";

            if (attrs.Any(a => a.AttributeType.FullName == TheoryAttributeName))
            {
                var rows = attrs.Where(a => a.AttributeType.FullName == InlineDataAttributeName).ToList();
                if (rows.Count == 0)
                {
                    cases.Add(new TestCase(method, Array.Empty<object?>(), name,
                        "source de données de [Theory] non supportée — utilise [InlineData] (ou convertis en [Fact])"));
                    continue;
                }

                foreach (var row in rows)
                {
                    var args = CoerceArgs(ExtractInlineData(row), method.GetParameters());
                    cases.Add(new TestCase(method, args, $"{name}({FormatArgs(args)})", null));
                }
            }
            else if (method.GetParameters().Length == 0
                && attrs.Any(a => a.AttributeType.FullName == FactAttributeName))
            {
                cases.Add(new TestCase(method, Array.Empty<object?>(), name, null));
            }
        }

        return cases;
    }

    private static object?[] ExtractInlineData(CustomAttributeData inlineData)
    {
        // ctor `InlineData(params object[] data)` → un seul argument constructeur : le tableau des valeurs.
        if (inlineData.ConstructorArguments.Count == 0)
        {
            return Array.Empty<object?>();
        }

        if (inlineData.ConstructorArguments[0].Value
            is IReadOnlyList<CustomAttributeTypedArgument> array)
        {
            return array.Select(a => a.Value).ToArray();
        }

        return inlineData.ConstructorArguments.Select(a => a.Value).ToArray();
    }

    private static object?[] CoerceArgs(object?[] rawArgs, ParameterInfo[] parameters)
    {
        var result = new object?[rawArgs.Length];
        for (var i = 0; i < rawArgs.Length; i++)
        {
            result[i] = i < parameters.Length ? Coerce(rawArgs[i], parameters[i].ParameterType) : rawArgs[i];
        }

        return result;
    }

    private static object? Coerce(object? value, Type target)
    {
        if (value is null)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(target) ?? target;
        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            return underlying.IsEnum
                ? Enum.ToObject(underlying, value)
                : Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return value; // types réellement incompatibles : laisser Invoke lever (compté comme échec).
        }
    }

    private static string FormatArgs(object?[] args) =>
        string.Join(", ", args.Select(a => a?.ToString() ?? "null"));

    private static string? RunOne(MethodInfo method, object?[] args)
    {
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(method.DeclaringType!);
            var result = method.Invoke(instance, args);
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
