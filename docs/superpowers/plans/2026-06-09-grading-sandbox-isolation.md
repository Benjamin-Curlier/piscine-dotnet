# Isolation de l'exécution recrue en processus enfant — Plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exécuter tout code recrue non fiable dans un processus enfant jetable, tué (arbre complet) au timeout, pour rendre une boucle infinie réellement terminable et supprimer fuites de thread/assembly et corruption de sortie inter-exécutions.

**Architecture:** Un nouvel exécutable `Piscine.Sandbox` détient toute l'exécution non fiable (chargement ALC + io + xunit + dispose). `ProgramRunner`/`XunitRunner` de `Piscine.Grading` deviennent des **clients** qui écrivent les octets + une requête JSON dans un dossier temp, lancent le bac à sable, appliquent le timeout, tuent l'arbre si dépassement, lisent un `result.json`, nettoient. Mort du processus = récupération totale ; aucune mutation du `Console` global côté parent. Fail-closed si le binaire est introuvable.

**Tech Stack:** .NET 10, C# (nullable + `TreatWarningsAsErrors`), xUnit 2.9.3, `System.Runtime.Loader.AssemblyLoadContext`, `System.Text.Json` (source-gen), `System.Diagnostics.Process` (`Kill(entireProcessTree:true)`), Roslyn (compilation existante, inchangée).

---

## Structure des fichiers

**Créer :**
- `src/Piscine.Sandbox/Piscine.Sandbox.csproj` — exe net10.0, refs `xunit.core` + `xunit.assert` uniquement.
- `src/Piscine.Sandbox/SandboxContract.cs` — `SandboxRequest`, `SandboxResult`, `SandboxJsonContext` (source-gen).
- `src/Piscine.Sandbox/SandboxExecutor.cs` — `Execute(request, bytes)` : charge l'ALC, dispatch io/xunit ; `RunIo`, `RunXunit`, `RunOne` (dispose en `finally`).
- `src/Piscine.Sandbox/SandboxEntry.cs` — `Run(workDir)` : lit la requête + asm, exécute, écrit `result.json` (+ hook `ProcessExit`).
- `src/Piscine.Sandbox/Program.cs` — `return SandboxEntry.Run(args[0]);`
- `src/Piscine.Grading/SandboxRunner.cs` — `SandboxUnavailableException`, `SandboxLauncher` (résolution du binaire), `SandboxProcess.Run` (spawn + timeout + kill + lecture résultat + nettoyage).
- `tests/Piscine.Grading.Tests/SandboxExecutorTests.cs`
- `tests/Piscine.Grading.Tests/SandboxEntryTests.cs`
- `tests/Piscine.Grading.Tests/ProgramRunnerTests.cs`
- `tests/Piscine.Grading.Tests/XunitRunnerTests.cs`
- `tests/Piscine.Grading.Tests/SandboxFailClosedTests.cs`

**Modifier :**
- `Piscine.slnx` — ajouter `Piscine.Sandbox`.
- `src/Piscine.Grading/Piscine.Grading.csproj` — `ProjectReference` → Sandbox.
- `tests/Piscine.Grading.Tests/Piscine.Grading.Tests.csproj` — `ProjectReference` → Sandbox.
- `src/Piscine.Grading/ProgramRunner.cs` — `RunOutcome.Error` → `RunError?` ; corps réécrit en client (Task 8).
- `src/Piscine.Grading/XunitRunner.cs` — corps `Run` réécrit en client (Task 9).
- `src/Piscine.Grading/IoGrader.cs`, `ReseauGrader.cs`, `ProjectGrader.cs`, `TryCommand.cs` — `run.Error.GetType().Name` → `run.Error.TypeName`.
- `src/Piscine.Grading/ExerciseGrader.cs` — try/catch fail-closed (Task 10).

**Note héritée :** `Directory.Build.props` applique `Nullable=enable` et `TreatWarningsAsErrors=true` à tous les projets — tout le code ci-dessous doit compiler sans avertissement.

---

## Task 1 : `RunError` + découpler `RunOutcome.Error` de `Exception`

But : un objet `Exception` ne traverse pas une frontière de processus. On remplace `RunOutcome.Error` par un enregistrement `RunError(TypeName, Message)` et on adapte les 4 sites d'appel, **en gardant `ProgramRunner` en in-process** (réécriture du corps en Task 8). À la fin, build + tests existants verts.

**Files:**
- Modify: `src/Piscine.Grading/ProgramRunner.cs`
- Modify: `src/Piscine.Grading/IoGrader.cs:48`
- Modify: `src/Piscine.Grading/ReseauGrader.cs:63`
- Modify: `src/Piscine.Grading/ProjectGrader.cs:110`
- Modify: `src/Piscine.Grading/TryCommand.cs:98`

- [ ] **Step 1 : Introduire `RunError` et changer `RunOutcome`**

Dans `src/Piscine.Grading/ProgramRunner.cs`, remplacer la ligne 10 :

```csharp
/// <summary>Erreur recrue rapportée à travers la frontière du bac à sable (type + message).</summary>
public sealed record RunError(string TypeName, string Message);

/// <summary>Issue d'une exécution isolée d'un assembly compilé.</summary>
public sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, RunError? Error);
```

- [ ] **Step 2 : Adapter le `catch` in-process de `ProgramRunner` pour produire un `RunError`**

Toujours dans `ProgramRunner.cs`, le bloc `Task.Run` capture aujourd'hui une `Exception? error`. Remplacer la variable et son usage (lignes ~42-64) pour produire un `RunError?` :

```csharp
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
```

Et le `entry is null` (ligne ~35) :

```csharp
                return new RunOutcome(string.Empty, 0, false, new RunError(nameof(InvalidOperationException), "Aucun point d'entrée (Main)."));
```

- [ ] **Step 3 : Adapter les 4 sites d'appel**

Dans chacun des 4 fichiers, remplacer `run.Error.GetType().Name` par `run.Error.TypeName`. Les lignes concernées :

`IoGrader.cs` (~48) :
```csharp
                return GraderResult.Failure(Type, $"Votre programme a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
                    .WithTrigger(FeedbackTriggers.RuntimeError);
```

`ReseauGrader.cs` (~63) :
```csharp
                return GraderResult.Failure(Type, $"Ton programme a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
                    .WithTrigger(FeedbackTriggers.RuntimeError);
```

`ProjectGrader.cs` (~110) :
```csharp
                return GraderResult.Failure(Type, $"Le projet a levé une exception : {run.Error.TypeName} — {run.Error.Message}")
                    .WithTrigger(FeedbackTriggers.RuntimeError);
```

`TryCommand.cs` (~98) :
```csharp
                output.AppendLine($"  ✗ exception : {run.Error.TypeName} — {run.Error.Message}");
```

- [ ] **Step 4 : Build + tests existants**

Run : `dotnet test tests/Piscine.Grading.Tests`
Expected : PASS (aucun test ne lisait `RunOutcome.Error` directement ; les graders compilent et passent comme avant).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading
git commit -m "refactor(grading): RunOutcome.Error -> RunError (préparer la frontière de processus)"
```

---

## Task 2 : Scaffolder le projet `Piscine.Sandbox` + câbler les références

But : créer le projet exe (contrat + squelettes), l'ajouter à la solution, et le référencer depuis `Piscine.Grading` et `Piscine.Grading.Tests` (co-localisation du binaire). Build vert.

**Files:**
- Create: `src/Piscine.Sandbox/Piscine.Sandbox.csproj`
- Create: `src/Piscine.Sandbox/SandboxContract.cs`
- Create: `src/Piscine.Sandbox/SandboxExecutor.cs`
- Create: `src/Piscine.Sandbox/SandboxEntry.cs`
- Create: `src/Piscine.Sandbox/Program.cs`
- Modify: `Piscine.slnx`
- Modify: `src/Piscine.Grading/Piscine.Grading.csproj`
- Modify: `tests/Piscine.Grading.Tests/Piscine.Grading.Tests.csproj`

- [ ] **Step 1 : csproj**

`src/Piscine.Sandbox/Piscine.Sandbox.csproj` :
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>Piscine.Sandbox</AssemblyName>
    <RootNamespace>Piscine.Sandbox</RootNamespace>
    <!-- Référence xunit.core pour EXÉCUTER les tests recrue, pas pour être un projet de tests. -->
    <IsTestProject>false</IsTestProject>
    <!-- Sérialisation JSON par source-gen : sûre sous trimming/AOT si un jour publié ainsi. -->
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.assert" Version="2.9.3" />
    <PackageReference Include="xunit.core" Version="2.9.3" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2 : Contrat + contexte JSON source-gen**

`src/Piscine.Sandbox/SandboxContract.cs` :
```csharp
using System.Text.Json.Serialization;

namespace Piscine.Sandbox;

/// <summary>Requête passée au bac à sable (sérialisée dans request.json).</summary>
public sealed class SandboxRequest
{
    /// <summary>"io" ou "xunit".</summary>
    public string Mode { get; set; } = "io";

    /// <summary>Arguments programme (mode io ; pour reseau, host/port en tête).</summary>
    public string[] Args { get; set; } = System.Array.Empty<string>();

    /// <summary>Entrée standard (mode io).</summary>
    public string Stdin { get; set; } = string.Empty;

    /// <summary>Chemins d'assemblies à résoudre par l'ALC (secours ; en général redondant).</summary>
    public string[] ReferencePaths { get; set; } = System.Array.Empty<string>();
}

/// <summary>Résultat produit par le bac à sable (sérialisé dans result.json).</summary>
public sealed class SandboxResult
{
    // Mode io
    public string Stdout { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }

    // Mode xunit
    public int FactCount { get; set; }
    public string[] Failures { get; set; } = System.Array.Empty<string>();

    // L'enfant est sorti tôt via Environment.Exit (résultat partiel).
    public bool ExitedEarly { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SandboxRequest))]
[JsonSerializable(typeof(SandboxResult))]
public partial class SandboxJsonContext : JsonSerializerContext;
```

- [ ] **Step 3 : Exécuteur (squelette qui lève) — implémenté en Tasks 3-4**

`src/Piscine.Sandbox/SandboxExecutor.cs` :
```csharp
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
```

- [ ] **Step 4 : Entrée + Program (squelette)**

`src/Piscine.Sandbox/SandboxEntry.cs` :
```csharp
namespace Piscine.Sandbox;

/// <summary>Point d'entrée logique du bac à sable : lit le dossier de travail, exécute, écrit result.json.</summary>
public static class SandboxEntry
{
    public static int Run(string workDir) => throw new System.NotImplementedException();
}
```

`src/Piscine.Sandbox/Program.cs` :
```csharp
if (args.Length < 1)
{
    System.Console.Error.WriteLine("usage: Piscine.Sandbox <workdir>");
    return 64;
}

return Piscine.Sandbox.SandboxEntry.Run(args[0]);
```

- [ ] **Step 5 : Ajouter à la solution**

Dans `Piscine.slnx`, ajouter sous `<Folder Name="/src/">` :
```xml
    <Project Path="src/Piscine.Sandbox/Piscine.Sandbox.csproj" />
```

- [ ] **Step 6 : Références projet**

Dans `src/Piscine.Grading/Piscine.Grading.csproj`, ajouter dans le premier `<ItemGroup>` (celui des `ProjectReference`) :
```xml
    <ProjectReference Include="..\Piscine.Sandbox\Piscine.Sandbox.csproj" />
```

Dans `tests/Piscine.Grading.Tests/Piscine.Grading.Tests.csproj`, ajouter dans l'`<ItemGroup>` des `ProjectReference` :
```xml
    <ProjectReference Include="..\..\src\Piscine.Sandbox\Piscine.Sandbox.csproj" />
```

- [ ] **Step 7 : Build**

Run : `dotnet build src/Piscine.Sandbox && dotnet build tests/Piscine.Grading.Tests`
Expected : PASS (le squelette compile ; `Execute` lèvera à l'exécution, non appelé pour l'instant).

- [ ] **Step 8 : Commit**

```bash
git add src/Piscine.Sandbox Piscine.slnx src/Piscine.Grading/Piscine.Grading.csproj tests/Piscine.Grading.Tests/Piscine.Grading.Tests.csproj
git commit -m "feat(sandbox): scaffolder Piscine.Sandbox + contrat IPC + références"
```

---

## Task 3 : `SandboxExecutor.RunXunit` + dispose en `finally` (TDD)

But : exécuter les `[Fact]` par réflexion et **disposer l'instance de test dans un `finally`** (`IDisposable`/`IAsyncDisposable`), y compris quand le `[Fact]` lève.

**Files:**
- Create: `tests/Piscine.Grading.Tests/SandboxExecutorTests.cs`
- Modify: `src/Piscine.Sandbox/SandboxExecutor.cs`

- [ ] **Step 1 : Test rouge — la fixture est disposée même si le test lève**

`tests/Piscine.Grading.Tests/SandboxExecutorTests.cs` :
```csharp
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Piscine.Sandbox;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxExecutorTests
{
    private static byte[] CompileXunit(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["Tests.cs"] = source },
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References).AssemblyBytes;

    [Fact]
    public void RunXunit_DisposesFixture_EvenWhenFactThrows()
    {
        var marker = Path.Combine(Path.GetTempPath(), $"sbx-dispose-{System.Guid.NewGuid():N}.txt");
        var markerLiteral = marker.Replace("\\", "\\\\");
        var source = $$"""
            using System;
            using System.IO;
            using Xunit;

            public class LeakyTests : IDisposable
            {
                [Fact]
                public void Boom() => throw new InvalidOperationException("boom");

                public void Dispose() => File.WriteAllText("{{markerLiteral}}", "disposed");
            }
            """;

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "xunit" }, CompileXunit(source));

        Assert.Equal(1, result.FactCount);
        Assert.Single(result.Failures);
        Assert.True(File.Exists(marker), "La fixture IDisposable n'a pas été disposée.");
        Assert.Equal("disposed", File.ReadAllText(marker));
        File.Delete(marker);
    }

    [Fact]
    public void RunXunit_ReportsPassAndFail()
    {
        var source = """
            using Xunit;
            public class T
            {
                [Fact] public void Ok() => Assert.True(true);
                [Fact] public void Ko() => Assert.Equal(1, 2);
            }
            """;

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "xunit" }, CompileXunit(source));

        Assert.Equal(2, result.FactCount);
        Assert.Single(result.Failures);
        Assert.Contains("Ko", result.Failures[0]);
    }
}
```

- [ ] **Step 2 : Lancer — rouge**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxExecutorTests.RunXunit"`
Expected : FAIL (`NotImplementedException` depuis `RunXunit`).

- [ ] **Step 3 : Implémenter `RunXunit` + `RunOne` + `FindFactMethods`**

Dans `src/Piscine.Sandbox/SandboxExecutor.cs`, remplacer le `RunXunit` stub et ajouter les helpers (ajouter `using System.Collections.Generic;`, `using System.Linq;`, `using System.Threading.Tasks;` en tête) :
```csharp
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
```

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxExecutorTests.RunXunit"`
Expected : PASS (les 2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Sandbox/SandboxExecutor.cs tests/Piscine.Grading.Tests/SandboxExecutorTests.cs
git commit -m "feat(sandbox): exécution xunit + dispose des fixtures en finally"
```

---

## Task 4 : `SandboxExecutor.RunIo` (TDD)

But : exécuter le point d'entrée, capturer stdout (Console redirigé localement, **restauré en `finally`** car appelable en proc dans les tests), code de sortie, et reporter une exception non rattrapée.

**Files:**
- Modify: `tests/Piscine.Grading.Tests/SandboxExecutorTests.cs`
- Modify: `src/Piscine.Sandbox/SandboxExecutor.cs`

- [ ] **Step 1 : Tests rouges — io**

Ajouter dans `SandboxExecutorTests` :
```csharp
    private static byte[] CompileIo(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["P.cs"] = source },
            OutputKind.ConsoleApplication).AssemblyBytes;

    [Fact]
    public void RunIo_CapturesStdout_AndExitCode()
    {
        var bytes = CompileIo("""
            System.Console.Write("Hello");
            return 7;
            """);

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io" }, bytes);

        Assert.Equal("Hello", result.Stdout);
        Assert.Equal(7, result.ExitCode);
        Assert.Null(result.ErrorType);
    }

    [Fact]
    public void RunIo_ReportsUncaughtException()
    {
        var bytes = CompileIo("""throw new System.InvalidOperationException("nope");""");

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io" }, bytes);

        Assert.Equal("InvalidOperationException", result.ErrorType);
        Assert.Equal("nope", result.ErrorMessage);
    }

    [Fact]
    public void RunIo_FeedsStdin()
    {
        var bytes = CompileIo("""System.Console.Write(System.Console.ReadLine());""");

        var result = SandboxExecutor.Execute(new SandboxRequest { Mode = "io", Stdin = "écho" }, bytes);

        Assert.Equal("écho", result.Stdout);
    }
```

- [ ] **Step 2 : Lancer — rouge**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxExecutorTests.RunIo"`
Expected : FAIL (`NotImplementedException`).

- [ ] **Step 3 : Implémenter `RunIo` + `InvokeEntry` + `Await`**

Dans `SandboxExecutor.cs`, remplacer le `RunIo` stub et ajouter les helpers :
```csharp
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
```

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxExecutorTests"`
Expected : PASS (io + xunit).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Sandbox/SandboxExecutor.cs tests/Piscine.Grading.Tests/SandboxExecutorTests.cs
git commit -m "feat(sandbox): exécution io (stdout/exit/exception/stdin)"
```

---

## Task 5 : `SandboxEntry.Run` round-trip + `Program` (TDD)

But : lire `request.json` + `asm.dll` d'un dossier, exécuter, écrire `result.json` ; hook `ProcessExit` pour `Environment.Exit`.

**Files:**
- Create: `tests/Piscine.Grading.Tests/SandboxEntryTests.cs`
- Modify: `src/Piscine.Sandbox/SandboxEntry.cs`

- [ ] **Step 1 : Test rouge — round-trip en proc**

`tests/Piscine.Grading.Tests/SandboxEntryTests.cs` :
```csharp
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Piscine.Sandbox;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxEntryTests
{
    [Fact]
    public void Run_ExecutesIo_AndWritesResultJson()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"sbx-entry-{System.Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);
        try
        {
            var bytes = CompilationService.Compile(
                new Dictionary<string, string> { ["P.cs"] = "System.Console.Write(\"OK\"); return 0;" },
                OutputKind.ConsoleApplication).AssemblyBytes;
            File.WriteAllBytes(Path.Combine(workDir, "asm.dll"), bytes);
            File.WriteAllText(
                Path.Combine(workDir, "request.json"),
                JsonSerializer.Serialize(new SandboxRequest { Mode = "io" }, SandboxJsonContext.Default.SandboxRequest));

            var code = SandboxEntry.Run(workDir);

            Assert.Equal(0, code);
            var resultPath = Path.Combine(workDir, "result.json");
            Assert.True(File.Exists(resultPath));
            var result = JsonSerializer.Deserialize(File.ReadAllText(resultPath), SandboxJsonContext.Default.SandboxResult)!;
            Assert.Equal("OK", result.Stdout);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }
}
```

- [ ] **Step 2 : Lancer — rouge**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxEntryTests"`
Expected : FAIL (`NotImplementedException`).

- [ ] **Step 3 : Implémenter `SandboxEntry.Run`**

`src/Piscine.Sandbox/SandboxEntry.cs` :
```csharp
using System;
using System.IO;
using System.Text.Json;

namespace Piscine.Sandbox;

/// <summary>Point d'entrée logique : lit le dossier de travail, exécute, écrit result.json.</summary>
public static class SandboxEntry
{
    public static int Run(string workDir)
    {
        var resultPath = Path.Combine(workDir, "result.json");
        var request = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(workDir, "request.json")),
            SandboxJsonContext.Default.SandboxRequest)!;
        var bytes = File.ReadAllBytes(Path.Combine(workDir, "asm.dll"));

        var result = new SandboxResult();
        var written = false;
        void Flush()
        {
            if (written)
            {
                return;
            }

            written = true;
            File.WriteAllText(resultPath, JsonSerializer.Serialize(result, SandboxJsonContext.Default.SandboxResult));
        }

        // Si la recrue appelle Environment.Exit(n), l'exécution ne revient pas : on tente d'écrire
        // un résultat partiel marqué ExitedEarly afin que le parent ne le prenne pas pour un crash.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            result.ExitedEarly = true;
            Flush();
        };

        try
        {
            result = SandboxExecutor.Execute(request, bytes);
        }
        catch (Exception ex)
        {
            result = new SandboxResult { ErrorType = ex.GetType().Name, ErrorMessage = ex.Message };
        }

        Flush();
        return 0;
    }
}
```

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxEntryTests"`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Sandbox/SandboxEntry.cs tests/Piscine.Grading.Tests/SandboxEntryTests.cs
git commit -m "feat(sandbox): SandboxEntry round-trip request/result + hook ProcessExit"
```

---

## Task 6 : `SandboxUnavailableException`, `SandboxLauncher`, `SandboxProcess` (client de lancement)

But : côté `Piscine.Grading`, résoudre le binaire du bac à sable, le lancer avec timeout, tuer l'arbre au dépassement, lire `result.json`, nettoyer, et **fail-closed** (lever) si le binaire est introuvable/illançable.

**Files:**
- Create: `src/Piscine.Grading/SandboxRunner.cs`

- [ ] **Step 1 : Écrire `SandboxRunner.cs`**

`src/Piscine.Grading/SandboxRunner.cs` :
```csharp
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>Le binaire du bac à sable est introuvable ou ne peut être lancé (erreur interne, fail-closed).</summary>
public sealed class SandboxUnavailableException : Exception
{
    public SandboxUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Résout la commande à lancer pour le bac à sable (surcharge env, apphost, ou dotnet+dll).</summary>
internal static class SandboxLauncher
{
    public static ProcessStartInfo CreateStartInfo(string workDir)
    {
        var (file, prefixArgs) = ResolveCommand();
        var psi = new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in prefixArgs)
        {
            psi.ArgumentList.Add(a);
        }

        psi.ArgumentList.Add(workDir);
        return psi;
    }

    private static (string File, string[] PrefixArgs) ResolveCommand()
    {
        var overridePath = Environment.GetEnvironmentVariable("PISCINE_SANDBOX");
        if (!string.IsNullOrEmpty(overridePath))
        {
            // Surcharge autoritaire : pas de repli. Un .dll ⇒ via le muxer dotnet ; sinon apphost direct.
            return overridePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? (DotnetMuxer(), new[] { overridePath })
                : (overridePath, Array.Empty<string>());
        }

        var baseDir = AppContext.BaseDirectory;
        var exeName = OperatingSystem.IsWindows() ? "Piscine.Sandbox.exe" : "Piscine.Sandbox";
        var apphost = Path.Combine(baseDir, exeName);
        if (File.Exists(apphost))
        {
            return (apphost, Array.Empty<string>());
        }

        var dll = Path.Combine(baseDir, "Piscine.Sandbox.dll");
        if (File.Exists(dll))
        {
            return (DotnetMuxer(), new[] { dll });
        }

        throw new SandboxUnavailableException(
            $"Binaire du bac à sable introuvable près de « {baseDir} » (ni {exeName} ni Piscine.Sandbox.dll).");
    }

    private static string DotnetMuxer()
    {
        var current = Environment.ProcessPath;
        if (current is not null
            && Path.GetFileNameWithoutExtension(current).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }

        var root = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(root))
        {
            var candidate = Path.Combine(root, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
    }
}

/// <summary>Lance le bac à sable dans un processus enfant jetable et renvoie son résultat.</summary>
internal static class SandboxProcess
{
    public static SandboxResult Run(SandboxRequest request, byte[] assemblyBytes, TimeSpan timeout, out bool timedOut)
    {
        timedOut = false;
        var workDir = Path.Combine(Path.GetTempPath(), "piscine-sbx", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            File.WriteAllBytes(Path.Combine(workDir, "asm.dll"), assemblyBytes);
            File.WriteAllText(
                Path.Combine(workDir, "request.json"),
                JsonSerializer.Serialize(request, SandboxJsonContext.Default.SandboxRequest));

            var psi = SandboxLauncher.CreateStartInfo(workDir);
            Process process;
            try
            {
                process = Process.Start(psi)
                    ?? throw new SandboxUnavailableException("Process.Start a renvoyé null pour le bac à sable.");
            }
            catch (Exception ex) when (ex is Win32Exception or FileNotFoundException or InvalidOperationException)
            {
                throw new SandboxUnavailableException($"Lancement du bac à sable impossible : {ex.Message}", ex);
            }

            using (process)
            {
                // Drainer stdout/stderr pour éviter un blocage de pipe (le protocole passe par fichier).
                process.OutputDataReceived += static (_, _) => { };
                process.ErrorDataReceived += static (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch { /* déjà mort */ }
                    process.WaitForExit();
                    timedOut = true;
                    return new SandboxResult();
                }

                process.WaitForExit(); // s'assurer que les handlers async ont vidé

                var resultPath = Path.Combine(workDir, "result.json");
                if (File.Exists(resultPath))
                {
                    var result = JsonSerializer.Deserialize(
                        File.ReadAllText(resultPath), SandboxJsonContext.Default.SandboxResult);
                    if (result is not null)
                    {
                        if (result.ExitedEarly)
                        {
                            result.ExitCode = process.ExitCode;
                        }

                        return result;
                    }
                }

                // Pas de result.json et pas de timeout ⇒ arrêt anormal (StackOverflow, FailFast…).
                return new SandboxResult
                {
                    ErrorType = "ArrêtAnormal",
                    ErrorMessage = $"Le bac à sable s'est arrêté anormalement (code {process.ExitCode}) sans produire de résultat.",
                };
            }
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); }
            catch { /* nettoyage best-effort */ }
        }
    }
}
```

- [ ] **Step 2 : Build**

Run : `dotnet build src/Piscine.Grading`
Expected : PASS.

- [ ] **Step 3 : Commit**

```bash
git add src/Piscine.Grading/SandboxRunner.cs
git commit -m "feat(grading): client de lancement du bac à sable (spawn + timeout + kill-tree + fail-closed)"
```

---

## Task 7 : Basculer `ProgramRunner.Run` vers le processus enfant (TDD — bug principal)

But : `ProgramRunner.Run` n'exécute plus en proc ; il délègue à `SandboxProcess`. Le test d'acceptation « boucle infinie puis exécution propre, sans corruption » est **rouge sur le code in-process actuel** et **vert** après bascule.

**Files:**
- Create: `tests/Piscine.Grading.Tests/ProgramRunnerTests.cs`
- Modify: `src/Piscine.Grading/ProgramRunner.cs`

- [ ] **Step 1 : Tests — parité + non-contamination**

`tests/Piscine.Grading.Tests/ProgramRunnerTests.cs` :
```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ProgramRunnerTests
{
    private static byte[] Compile(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["P.cs"] = source },
            OutputKind.ConsoleApplication).AssemblyBytes;

    [Fact]
    public void Run_ReturnsStdoutAndExitCode()
    {
        var run = ProgramRunner.Run(Compile("System.Console.Write(\"hi\"); return 4;"),
            Array.Empty<string>(), stdin: "");

        Assert.False(run.TimedOut);
        Assert.Equal("hi", run.Stdout);
        Assert.Equal(4, run.ExitCode);
        Assert.Null(run.Error);
    }

    [Fact]
    public void Run_ReportsException()
    {
        var run = ProgramRunner.Run(Compile("throw new System.InvalidOperationException(\"x\");"),
            Array.Empty<string>(), stdin: "");

        Assert.NotNull(run.Error);
        Assert.Equal("InvalidOperationException", run.Error!.TypeName);
    }

    [Fact]
    public void Run_InfiniteLoop_TimesOut_WithoutCorruptingNextRun()
    {
        // Boucle infinie qui écrit en continu : sous l'ancien modèle in-process, la tâche orpheline
        // survit au timeout et écrit dans le StringWriter de l'exécution SUIVANTE (contamination).
        var loop = Compile("""
            while (true) { System.Console.Write("X"); System.Threading.Thread.Sleep(1); }
            """);
        var clean = Compile("""System.Console.Write("propre");""");

        var first = ProgramRunner.Run(loop, Array.Empty<string>(), stdin: "", TimeSpan.FromMilliseconds(500));
        Assert.True(first.TimedOut);

        // Laisser une fenêtre pendant laquelle une éventuelle orpheline pourrait corrompre la suite.
        System.Threading.Thread.Sleep(300);

        var second = ProgramRunner.Run(clean, Array.Empty<string>(), stdin: "", TimeSpan.FromSeconds(5));
        Assert.False(second.TimedOut);
        Assert.Equal("propre", second.Stdout); // aucun "X" parasite
    }
}
```

- [ ] **Step 2 : Lancer — état rouge attendu**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~ProgramRunnerTests"`
Expected : `Run_InfiniteLoop_TimesOut_WithoutCorruptingNextRun` FAIL (sortie de la 2ᵉ exécution contaminée par des « X »), les autres PASS. (Si l'orpheline tourne encore, l'arrêter en relançant le test host ; c'est précisément le bug qu'on supprime.)

- [ ] **Step 3 : Réécrire `ProgramRunner.Run` en client**

Remplacer l'intégralité du corps utile de `src/Piscine.Grading/ProgramRunner.cs` (garder `RunError`/`RunOutcome` de Task 1, supprimer `InvokeEntry`/`Await`/`Task`/`AssemblyLoadContext` désormais inutiles) :
```csharp
using System;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>Erreur recrue rapportée à travers la frontière du bac à sable (type + message).</summary>
public sealed record RunError(string TypeName, string Message);

/// <summary>Issue d'une exécution isolée d'un assembly compilé.</summary>
public sealed record RunOutcome(string Stdout, int ExitCode, bool TimedOut, RunError? Error);

/// <summary>
/// Exécute un assembly console compilé dans un PROCESSUS ENFANT jetable (Piscine.Sandbox), tué au
/// timeout. Partagé par les graders io/projet/reseau et l'outil auteur <c>piscine try</c>.
/// </summary>
public static class ProgramRunner
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static RunOutcome Run(byte[] assemblyBytes, string[] args, string stdin, TimeSpan? timeout = null)
    {
        var request = new SandboxRequest { Mode = "io", Args = args, Stdin = stdin };
        var result = SandboxProcess.Run(request, assemblyBytes, timeout ?? DefaultTimeout, out var timedOut);
        if (timedOut)
        {
            return new RunOutcome(string.Empty, 0, true, null);
        }

        var error = result.ErrorType is null ? null : new RunError(result.ErrorType, result.ErrorMessage ?? string.Empty);
        return new RunOutcome(result.Stdout, result.ExitCode, false, error);
    }
}
```

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~ProgramRunnerTests"`
Expected : PASS (les 3, dont la non-contamination : le processus enfant est tué, aucune orpheline ne survit).

- [ ] **Step 5 : Régression io/projet/reseau**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~IoGraderTests|FullyQualifiedName~ProjectGraderTests|FullyQualifiedName~ReseauGraderTests|FullyQualifiedName~TryCommandTests"`
Expected : PASS.

- [ ] **Step 6 : Commit**

```bash
git add src/Piscine.Grading/ProgramRunner.cs tests/Piscine.Grading.Tests/ProgramRunnerTests.cs
git commit -m "feat(grading): ProgramRunner exécute en processus enfant (boucle infinie terminable, zéro contamination)"
```

---

## Task 8 : Basculer `XunitRunner.Run` vers le processus enfant (TDD)

But : `XunitRunner.Run` délègue à `SandboxProcess` (mode xunit). Garder `XunitRunner.References` et le type `RunResult`. Le timeout xunit tue l'arbre.

**Files:**
- Create: `tests/Piscine.Grading.Tests/XunitRunnerTests.cs`
- Modify: `src/Piscine.Grading/XunitRunner.cs`

- [ ] **Step 1 : Tests — parité + timeout + dispose bout-en-bout**

`tests/Piscine.Grading.Tests/XunitRunnerTests.cs` :
```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class XunitRunnerTests
{
    private static byte[] Compile(string source) =>
        CompilationService.Compile(
            new Dictionary<string, string> { ["Tests.cs"] = source },
            OutputKind.DynamicallyLinkedLibrary,
            additionalReferences: XunitRunner.References).AssemblyBytes;

    [Fact]
    public void Run_CountsFactsAndFailures()
    {
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                [Fact] public void A() => Assert.True(true);
                [Fact] public void B() => Assert.Equal(1, 2);
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.Equal(2, run.FactCount);
        Assert.Single(run.Failures);
    }

    [Fact]
    public void Run_InfiniteLoopInFact_TimesOut()
    {
        var run = XunitRunner.Run(Compile("""
            using Xunit;
            public class T
            {
                [Fact] public void Loop() { while (true) { System.Threading.Thread.Sleep(1); } }
            }
            """), TimeSpan.FromMilliseconds(500));

        Assert.True(run.TimedOut);
    }
}
```

- [ ] **Step 2 : Lancer — rouge**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~XunitRunnerTests"`
Expected : FAIL (XunitRunner exécute encore en proc — `Run_InfiniteLoopInFact_TimesOut` peut laisser une orpheline ; c'est le bug). Selon l'environnement, parité PASS, timeout problématique.

- [ ] **Step 3 : Réécrire `XunitRunner.Run` en client**

Remplacer `src/Piscine.Grading/XunitRunner.cs` (garder `References` et `RunResult`, supprimer la logique ALC/Task/réflexion déplacée dans le bac à sable) :
```csharp
using System;
using Piscine.Sandbox;

namespace Piscine.Grading;

/// <summary>
/// Exécute les méthodes <c>[Fact]</c> d'un assembly compilé dans un PROCESSUS ENFANT jetable
/// (Piscine.Sandbox), tué au timeout. Partagé par les graders <c>unit</c> et <c>mutation</c>.
/// </summary>
internal static class XunitRunner
{
    /// <summary>Chemins des assemblies xUnit à passer en références de compilation.</summary>
    public static readonly string[] References =
    {
        typeof(Xunit.Assert).Assembly.Location,
        typeof(Xunit.FactAttribute).Assembly.Location,
    };

    /// <summary>Résultat d'une exécution : nombre de tests trouvés, échecs, et drapeau de timeout.</summary>
    public sealed record RunResult(int FactCount, IReadOnlyList<string> Failures, bool TimedOut);

    public static RunResult Run(byte[] assemblyBytes, TimeSpan timeout)
    {
        var request = new SandboxRequest { Mode = "xunit" };
        var result = SandboxProcess.Run(request, assemblyBytes, timeout, out var timedOut);
        return timedOut
            ? new RunResult(0, Array.Empty<string>(), true)
            : new RunResult(result.FactCount, result.Failures, false);
    }
}
```

Note : ajouter `using System.Collections.Generic;` en tête (utilisé par `IReadOnlyList`).

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~XunitRunnerTests|FullyQualifiedName~UnitGraderTests|FullyQualifiedName~MutationGraderTests"`
Expected : PASS (parité, timeout tué proprement, et les graders unit/mutation bout-en-bout).

- [ ] **Step 5 : Dispose bout-en-bout via `UnitGrader`**

Ajouter dans `XunitRunnerTests` un test qui prouve le dispose à travers le pipeline complet (processus enfant) :
```csharp
    [Fact]
    public void Run_DisposesFixture_EndToEnd()
    {
        var marker = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sbx-e2e-{Guid.NewGuid():N}.txt");
        var lit = marker.Replace("\\", "\\\\");
        var run = XunitRunner.Run(Compile($$"""
            using System;
            using System.IO;
            using Xunit;
            public class T : IDisposable
            {
                [Fact] public void A() => Assert.True(true);
                public void Dispose() => File.WriteAllText("{{lit}}", "ok");
            }
            """), TimeSpan.FromSeconds(10));

        Assert.False(run.TimedOut);
        Assert.True(System.IO.File.Exists(marker), "Fixture non disposée à travers le processus enfant.");
        System.IO.File.Delete(marker);
    }
```
Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~XunitRunnerTests.Run_DisposesFixture_EndToEnd"`
Expected : PASS.

- [ ] **Step 6 : Commit**

```bash
git add src/Piscine.Grading/XunitRunner.cs tests/Piscine.Grading.Tests/XunitRunnerTests.cs
git commit -m "feat(grading): XunitRunner exécute en processus enfant (timeout terminable, fixtures disposées)"
```

---

## Task 9 : Fail-closed dans `ExerciseGrader` si bac à sable indisponible (TDD)

But : si un grader lève `SandboxUnavailableException` (binaire introuvable/illançable), `ExerciseGrader` renvoie un échec « interne » explicite — ni faux « Réussi », ni repli in-process.

**Files:**
- Create: `tests/Piscine.Grading.Tests/SandboxFailClosedTests.cs`
- Modify: `src/Piscine.Grading/ExerciseGrader.cs`

- [ ] **Step 1 : Test rouge — un grader qui lève donne un échec interne**

`tests/Piscine.Grading.Tests/SandboxFailClosedTests.cs` :
```csharp
using System.Collections.Generic;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class SandboxFailClosedTests
{
    private sealed class ThrowingGrader : IGrader
    {
        public string Type => "io";
        public GraderResult Grade(GradingContext context, GradingStep step) =>
            throw new SandboxUnavailableException("binaire absent");
    }

    [Fact]
    public void Grade_SandboxUnavailable_FailsClosed_WithInternalError()
    {
        var grader = new ExerciseGrader(new IGrader[] { new ThrowingGrader() });
        var manifest = new ExerciseManifest
        {
            Id = "ex",
            Grading = { new GradingStep { Type = "io" } },
        };

        var result = grader.Grade(manifest, new GradingContext(new Dictionary<string, string>()));

        var io = Assert.Single(result.Results);
        Assert.Equal(GraderStatus.ARevoir, io.Status);
        Assert.Contains(io.Messages, m => m.Contains("interne", System.StringComparison.OrdinalIgnoreCase));
    }
}
```

(Noms confirmés : `ExerciseManifest.Id`/`Grading`, `ExerciseGradingResult.Results`, `GradingStep.Type`.)

- [ ] **Step 2 : Lancer — rouge**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxFailClosedTests"`
Expected : FAIL (l'exception se propage hors de `Grade`).

- [ ] **Step 3 : Try/catch fail-closed dans `ExerciseGrader.Grade`**

Dans `src/Piscine.Grading/ExerciseGrader.cs`, remplacer le bloc `if (_graders.TryGetValue(...))` (lignes ~25-38) :
```csharp
            if (_graders.TryGetValue(step.Type, out var grader))
            {
                try
                {
                    results.Add(grader.Grade(context, step));
                }
                catch (SandboxUnavailableException ex)
                {
                    // Fail-closed : le bac à sable d'exécution est indisponible (packaging cassé,
                    // binaire absent). On NE retombe PAS en in-process (cela réintroduirait les fuites
                    // et masquerait la casse) et on NE laisse PAS « réussir » : échec interne explicite.
                    results.Add(GraderResult.Failure(step.Type,
                        $"interne : bac à sable d'exécution indisponible — {ex.Message}"));
                }
            }
            else
```

- [ ] **Step 4 : Lancer — vert**

Run : `dotnet test tests/Piscine.Grading.Tests --filter "FullyQualifiedName~SandboxFailClosedTests"`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Grading/ExerciseGrader.cs tests/Piscine.Grading.Tests/SandboxFailClosedTests.cs
git commit -m "feat(grading): fail-closed sur bac à sable indisponible (échec interne, pas de repli)"
```

---

## Task 10 : Vérification complète + résolution multi-environnements + retex

But : prouver que tout est vert sur les trois chemins qui comptent (`dotnet test`, `dotnet run` DevHost, `dotnet publish` CLI) et que `validate-content` passe. Consigner.

**Files:**
- Create: `docs/superpowers/retex/2026-06-09-grading-sandbox-isolation.md`
- (potentiellement) Modify: `docs/superpowers/HANDOFF.md`

- [ ] **Step 1 : Suite complète**

Run : `dotnet test`
Expected : PASS (tous projets de tests ; la grille sandbox est co-localisée dans chaque sortie de test via les références transitives).
Si `XunitRunnerTests`/`ProgramRunnerTests` échouent par binaire introuvable, vérifier que `tests/Piscine.Grading.Tests/bin/.../Piscine.Sandbox.dll` existe ; sinon utiliser la surcharge dans un fixture (voir Step 4).

- [ ] **Step 2 : `validate-content` via la CLI**

Run :
```bash
dotnet run --project src/Piscine.Cli -- validate-content
```
(avec `PISCINE_CONTENT` pointant sur `content/` si nécessaire — cf. conventions du dépôt).
Expected : sortie de validation inchangée (les corrigés de référence passent), code de sortie 0. Confirme que la CLI localise et lance le bac à sable depuis sa sortie `bin`.

- [ ] **Step 3 : Smoke `dotnet run` DevHost (résolution sous serveur web)**

Run : `dotnet test tests/Piscine.DevHost.E2E --filter "FullyQualifiedName~CheckSmokeTests"`
Expected : PASS ou skip propre (si Chromium absent). Si PASS, prouve que le DevHost lancé via `dotnet run` localise le bac à sable et corrige bout-en-bout.

- [ ] **Step 4 : Confirmer la résolution du binaire (apphost vs dotnet+dll)**

Vérifier quel chemin de `SandboxLauncher.ResolveCommand` est emprunté dans la sortie de test/publish :
```bash
ls tests/Piscine.Grading.Tests/bin/Debug/net10.0/Piscine.Sandbox.*
dotnet publish src/Piscine.Cli -c Release -o /tmp/cli-pub && ls /tmp/cli-pub/Piscine.Sandbox.*
```
- Si l'apphost `Piscine.Sandbox(.exe)` est présent ⇒ chemin direct, idéal.
- Sinon, seul `Piscine.Sandbox.dll` (+ `.runtimeconfig.json`) ⇒ repli `dotnet exec` ; s'assurer que `dotnet` est résoluble (PATH/`DOTNET_ROOT`).
- En CI/packaging, si le binaire venait à manquer dans un artefact, ajouter une **assertion de co-localisation** (sur le modèle des assertions de libs natives du retex PhotinoX) qui échoue le build d'artefact.

- [ ] **Step 5 : Retex**

Écrire `docs/superpowers/retex/2026-06-09-grading-sandbox-isolation.md` : objectif, ce qui a marché, surprises (notamment le chemin de résolution réellement emprunté apphost/dll par environnement), résultat. Mettre à jour `docs/superpowers/HANDOFF.md` si le dépôt l'exige.

- [ ] **Step 6 : Commit**

```bash
git add docs/superpowers
git commit -m "docs(grading): retex isolation processus enfant + vérif multi-environnements"
```

---

## Self-review (auteur)

**Couverture spec :**
- Processus enfant jetable + kill-tree → Tasks 6-8. ✓
- `RunOutcome.Error` → `RunError` → Task 1. ✓
- Contrat IPC (fichiers request/asm/result, DTO) → Tasks 2, 5, 6. ✓
- Dispose en `finally` (IDisposable + IAsyncDisposable) → Task 3 + test e2e Task 8. ✓
- Abandon des parades in-process (AsyncLocal Console, ContinueWith) → confirmé : aucune n'est implémentée ; seul `RunIo` touche le `Console` localement et le restaure (justifié, exécution synchrone). ✓
- Fail-closed binaire absent → Tasks 6 (lève) + 9 (capture → échec interne). ✓
- Résolution du lanceur (override/apphost/dotnet+dll) → Task 6 + vérif Task 10. ✓
- Sémantique `Environment.Exit`/arrêt anormal → hook `ProcessExit` (Task 5) + synthèse abnormal (Task 6). ✓
- Tests d'acceptation (non-contamination, fixture disposée) → Tasks 7, 8. ✓
- Tests existants verts + validate-content → Tasks 7, 8, 10. ✓

**Scan placeholders :** aucun TBD/TODO ; tout le code est fourni. Une note de vérification de noms (Task 9 Step 1) renvoie à des fichiers précis à lire — ce n'est pas un placeholder de code mais une garde d'exactitude.

**Cohérence des types :** `SandboxRequest`/`SandboxResult`/`SandboxJsonContext` (Task 2) utilisés identiquement Tasks 3-8 ; `RunError(TypeName, Message)` (Task 1) lu en `.TypeName`/`.Message` partout ; `SandboxProcess.Run(request, bytes, timeout, out timedOut)` signature constante Tasks 6-8 ; `SandboxUnavailableException` (Task 6) capturée Task 9. ✓
