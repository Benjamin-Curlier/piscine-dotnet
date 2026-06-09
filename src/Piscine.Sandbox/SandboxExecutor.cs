using System;
using System.Reflection;
using System.Runtime.Loader;

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

    private static SandboxResult RunIo(Assembly assembly, string[] args, string stdin) =>
        throw new NotImplementedException();

    private static SandboxResult RunXunit(Assembly assembly) =>
        throw new NotImplementedException();
}
