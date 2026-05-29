using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Piscine.Grading;

/// <summary>Résultat d'une compilation Roslyn.</summary>
public sealed class CompilationResult
{
    private CompilationResult(bool success, byte[] assemblyBytes, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        Success = success;
        AssemblyBytes = assemblyBytes;
        Errors = errors;
        Warnings = warnings;
    }

    public bool Success { get; }

    public byte[] AssemblyBytes { get; }

    public IReadOnlyList<string> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public static CompilationResult Ok(byte[] bytes, IReadOnlyList<string> warnings) =>
        new(true, bytes, new List<string>(), warnings);

    public static CompilationResult Failed(IReadOnlyList<string> errors) =>
        new(false, Array.Empty<byte>(), errors, new List<string>());
}

/// <summary>Compile des sources C# en mémoire via Roslyn.</summary>
public static class CompilationService
{
    public static CompilationResult Compile(
        IReadOnlyDictionary<string, string> sources,
        OutputKind outputKind,
        string assemblyName = "Submission")
    {
        var syntaxTrees = sources
            .Select(kv => CSharpSyntaxTree.ParseText(kv.Value, path: kv.Key))
            .ToList();

        var options = new CSharpCompilationOptions(outputKind);
        var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, References.Value, options);

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);

        if (!emit.Success)
        {
            var errors = emit.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(Format)
                .ToList();
            return CompilationResult.Failed(errors);
        }

        var warnings = emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Warning)
            .Select(Format)
            .ToList();
        return CompilationResult.Ok(ms.ToArray(), warnings);
    }

    private static string Format(Diagnostic diagnostic)
    {
        var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
        return $"ligne {line} : {diagnostic.GetMessage()}";
    }

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(LoadReferences);

    private static IReadOnlyList<MetadataReference> LoadReferences()
    {
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;
        return tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
    }
}
