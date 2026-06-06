using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Piscine.Grading;

/// <summary>
/// Exécute les méthodes <c>[Fact]</c> d'un assembly compilé, dans un <see cref="AssemblyLoadContext"/>
/// collectible. Partagé par les graders <c>unit</c> et <c>mutation</c>.
/// </summary>
internal static class XunitRunner
{
    /// <summary>Chemins des assemblies xUnit à passer en références de compilation.</summary>
    public static readonly string[] References =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location
    };

    /// <summary>Résultat d'une exécution : nombre de tests trouvés, échecs, et drapeau de timeout.</summary>
    public sealed record RunResult(int FactCount, IReadOnlyList<string> Failures, bool TimedOut);

    public static RunResult Run(byte[] assemblyBytes, TimeSpan timeout)
    {
        var alc = new AssemblyLoadContext("xunit-run", isCollectible: true);
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var methods = FindFactMethods(assembly);
            if (methods.Count == 0)
            {
                return new RunResult(0, Array.Empty<string>(), false);
            }

            var failures = new List<string>();
            var task = Task.Run(() =>
            {
                foreach (var method in methods)
                {
                    var error = RunOne(method);
                    if (error is not null)
                    {
                        failures.Add($"{method.DeclaringType?.Name}.{method.Name} : {error}");
                    }
                }
            });

            return task.Wait(timeout)
                ? new RunResult(methods.Count, failures, false)
                : new RunResult(methods.Count, Array.Empty<string>(), true);
        }
        finally
        {
            alc.Unload();
        }
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
        try
        {
            var instance = Activator.CreateInstance(method.DeclaringType!);
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
    }
}
