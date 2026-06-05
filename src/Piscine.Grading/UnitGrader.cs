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
/// Grader <c>unit</c> : compile les sources de la recrue avec des tests xUnit cachés,
/// puis exécute les méthodes <c>[Fact]</c> par réflexion (un échec d'assertion = test KO).
/// </summary>
public sealed class UnitGrader : IGrader
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    // Force le chargement des assemblies xUnit et fournit leurs chemins comme références.
    private static readonly string[] XunitReferences =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location
    };

    public string Type => "unit";

    public GraderResult Grade(GradingContext context, GradingStep step)
    {
        var sources = new Dictionary<string, string>(context.Sources);
        foreach (var (name, content) in context.GraderFiles)
        {
            sources[name] = content;
        }

        var compilation = CompilationService.Compile(
            sources,
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitReferences);

        if (!compilation.Success)
        {
            var messages = new List<string> { "Le code ne compile pas :" };
            messages.AddRange(compilation.Errors);
            return GraderResult.Failure(Type, messages.ToArray()).WithTrigger(FeedbackTriggers.CompileError);
        }

        return RunTests(compilation.AssemblyBytes);
    }

    private GraderResult RunTests(byte[] assemblyBytes)
    {
        var alc = new AssemblyLoadContext("unit-tests", isCollectible: true);
        try
        {
            using var ms = new MemoryStream(assemblyBytes);
            var assembly = alc.LoadFromStream(ms);
            var methods = FindFactMethods(assembly);

            if (methods.Count == 0)
            {
                return GraderResult.Failure(Type, "Aucun test n'a été trouvé.").WithTrigger(FeedbackTriggers.UnitFailure);
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

            if (!task.Wait(Timeout))
            {
                return GraderResult.Failure(Type, "Les tests ne se sont pas terminés à temps (boucle infinie ?).")
                    .WithTrigger(FeedbackTriggers.Timeout);
            }

            return failures.Count == 0
                ? GraderResult.Success(Type)
                : GraderResult.Failure(Type, failures.ToArray()).WithTrigger(FeedbackTriggers.UnitFailure);
        }
        finally
        {
            alc.Unload();
        }
    }

    private static List<MethodInfo> FindFactMethods(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetParameters().Length == 0
                && m.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "Xunit.FactAttribute"))
            .ToList();
    }

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
            var inner = ex.InnerException ?? ex;
            return inner.Message;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
