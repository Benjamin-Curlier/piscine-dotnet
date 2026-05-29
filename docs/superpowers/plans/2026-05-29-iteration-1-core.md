# Itération 1 — Core — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Doter `Piscine.Core` du modèle de domaine, du chargement YAML des modules/exercices, de la découverte de contenu ordonnée, et d'un store de progression JSON — le tout en TDD.

**Architecture:** Des DTO simples (`Module`, `ExerciseGroup`, `ExerciseManifest`) désérialisés depuis YAML via un désérialiseur partagé (`YamlLoader`, `UnderscoredNamingConvention`). Des loaders à responsabilité unique (`ModuleLoader`, `ExerciseManifestLoader`) lisent un fichier ; `ContentDiscovery` scanne `content/modules` et renvoie les modules triés par `order`. `ProgressStore` (JSON System.Text.Json) lit/écrit la progression. Tous les tests sont hermétiques : ils écrivent des fichiers YAML/JSON dans un dossier temporaire jetable.

**Tech Stack:** .NET 10, C#, YamlDotNet, System.Text.Json (intégré), xUnit v2.

**Contexte repo (It.0 faite) :** `Piscine.slnx`, projets `src/Piscine.Core` et `tests/Piscine.Core.Tests` existants. `Directory.Build.props` impose `Nullable=enable` + `TreatWarningsAsErrors=true` → **toutes les propriétés string des DTO sont initialisées à `string.Empty` et les collections à `new()`** pour éviter CS8618. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Chaque commit finit par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Core/Model/Module.cs` | DTO d'un module (`module.yaml`) + ses groupes |
| `src/Piscine.Core/Model/ExerciseGroup.cs` | DTO d'un groupe d'exercices ordonné |
| `src/Piscine.Core/Model/ExerciseManifest.cs` | DTO d'un exercice (`manifest.yaml`, sous-ensemble structurel) |
| `src/Piscine.Core/Model/ExerciseStatus.cs` | Enum des statuts de progression persistés |
| `src/Piscine.Core/Model/Progress.cs` | DTO de progression (dictionnaire d'`ExerciseProgress`) |
| `src/Piscine.Core/Io/YamlLoader.cs` | Désérialiseur YAML partagé + lecture fichier |
| `src/Piscine.Core/Content/ModuleLoader.cs` | Charge un `module.yaml` en `Module` |
| `src/Piscine.Core/Content/ExerciseManifestLoader.cs` | Charge un `manifest.yaml` en `ExerciseManifest` |
| `src/Piscine.Core/Content/ContentDiscovery.cs` | Scanne `modules/`, renvoie les `Module` triés par `order` |
| `src/Piscine.Core/Progress/ProgressStore.cs` | Charge/sauve la progression en JSON |
| `tests/Piscine.Core.Tests/TempDir.cs` | Utilitaire de test : dossier temporaire jetable |
| `tests/Piscine.Core.Tests/*Tests.cs` | Tests par composant |

> Les détails de `grading`/`feedback`/`constraints` du manifest sont **hors périmètre It.1** (consommés à l'It.2 par le moteur de notation). On parse ici le sous-ensemble structurel nécessaire à la découverte et à l'affichage.

---

## Task 1 : Dépendance YamlDotNet + utilitaire de test TempDir

**Files:**
- Modify: `src/Piscine.Core/Piscine.Core.csproj`
- Create: `tests/Piscine.Core.Tests/TempDir.cs`

- [ ] **Step 1 : Ajouter le package YamlDotNet à Core**

Run: `dotnet add src/Piscine.Core package YamlDotNet`
Expected : « PackageReference for package 'YamlDotNet' ... added ».

- [ ] **Step 2 : Vérifier que la solution compile toujours**

Run: `dotnet build Piscine.slnx`
Expected : PASS — « Build succeeded », 0 erreur.

- [ ] **Step 3 : Créer l'utilitaire `TempDir`** (dossier temporaire auto-supprimé, pour tests hermétiques)

`tests/Piscine.Core.Tests/TempDir.cs` :
```csharp
using System;
using System.IO;

namespace Piscine.Core.Tests;

/// <summary>
/// Dossier temporaire unique, supprimé à la libération. Pour tests hermétiques sur fichiers.
/// </summary>
public sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "piscine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string relativePath, string content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // best-effort : un fichier verrouillé ne doit pas faire échouer le test
        }
    }
}
```

- [ ] **Step 4 : Vérifier la compilation des tests**

Run: `dotnet build tests/Piscine.Core.Tests`
Expected : PASS — 0 erreur.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Piscine.Core.csproj tests/Piscine.Core.Tests/TempDir.cs
git commit -m "chore(core): dependance YamlDotNet et utilitaire de test TempDir

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : DTO du modèle de domaine

**Files:**
- Create: `src/Piscine.Core/Model/ExerciseGroup.cs`
- Create: `src/Piscine.Core/Model/Module.cs`
- Create: `src/Piscine.Core/Model/ExerciseManifest.cs`

> Pas de test dédié ici (DTO sans logique) ; ils sont couverts par les tests des loaders (Tasks 4-6). On vérifie juste la compilation.

- [ ] **Step 1 : Créer `ExerciseGroup`**

`src/Piscine.Core/Model/ExerciseGroup.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Un groupe d'exercices ordonné (l'ordre = correction séquentielle, stop au 1er KO).</summary>
public sealed class ExerciseGroup
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<string> Exercises { get; set; } = new();
}
```

- [ ] **Step 2 : Créer `Module`**

`src/Piscine.Core/Model/Module.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Un module pédagogique, désérialisé depuis <c>module.yaml</c>.</summary>
public sealed class Module
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int Order { get; set; }

    public string Course { get; set; } = string.Empty;

    public List<ExerciseGroup> Groups { get; set; } = new();
}
```

- [ ] **Step 3 : Créer `ExerciseManifest`**

`src/Piscine.Core/Model/ExerciseManifest.cs` :
```csharp
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>
/// Sous-ensemble structurel d'un exercice, désérialisé depuis <c>manifest.yaml</c>.
/// Les sections grading/feedback/constraints sont ajoutées à l'It.2.
/// </summary>
public sealed class ExerciseManifest
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public List<string> Deliverables { get; set; } = new();

    public List<string> Starter { get; set; } = new();
}
```

- [ ] **Step 4 : Vérifier la compilation**

Run: `dotnet build src/Piscine.Core`
Expected : PASS — 0 erreur.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Model/ExerciseGroup.cs src/Piscine.Core/Model/Module.cs src/Piscine.Core/Model/ExerciseManifest.cs
git commit -m "feat(core): DTO Module, ExerciseGroup, ExerciseManifest

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : Désérialiseur YAML partagé (`YamlLoader`)

**Files:**
- Test: `tests/Piscine.Core.Tests/YamlLoaderTests.cs`
- Create: `src/Piscine.Core/Io/YamlLoader.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/YamlLoaderTests.cs` :
```csharp
using Piscine.Core.Io;
using Piscine.Core.Model;
using Xunit;

namespace Piscine.Core.Tests;

public class YamlLoaderTests
{
    [Fact]
    public void Load_MapsUnderscoredKeysToProperties()
    {
        using var dir = new TempDir();
        var file = dir.WriteFile("manifest.yaml", """
            id: ex00-hello
            title: "Hello"
            objective: "Afficher un message"
            deliverables: [Hello.cs]
            """);

        var manifest = YamlLoader.Load<ExerciseManifest>(file);

        Assert.Equal("ex00-hello", manifest.Id);
        Assert.Equal("Hello", manifest.Title);
        Assert.Equal("Afficher un message", manifest.Objective);
        Assert.Equal(new[] { "Hello.cs" }, manifest.Deliverables);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter YamlLoaderTests`
Expected : FAIL — « The name 'YamlLoader' ... could not be found » (erreur de compilation).

- [ ] **Step 3 : Implémenter `YamlLoader`**

`src/Piscine.Core/Io/YamlLoader.cs` :
```csharp
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Piscine.Core.Io;

/// <summary>Désérialise des fichiers YAML de contenu (clés en underscore → propriétés PascalCase).</summary>
public static class YamlLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static T Load<T>(string path)
    {
        var yaml = File.ReadAllText(path);
        return Deserialize<T>(yaml);
    }

    public static T Deserialize<T>(string yaml) => Deserializer.Deserialize<T>(yaml);
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter YamlLoaderTests`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Io/YamlLoader.cs tests/Piscine.Core.Tests/YamlLoaderTests.cs
git commit -m "feat(core): YamlLoader desercialise le YAML de contenu

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `ModuleLoader`

**Files:**
- Test: `tests/Piscine.Core.Tests/ModuleLoaderTests.cs`
- Create: `src/Piscine.Core/Content/ModuleLoader.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/ModuleLoaderTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ModuleLoaderTests
{
    [Fact]
    public void Load_ParsesModuleWithGroupsAndOrderedExercises()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("00-setup-git", "module.yaml"), """
            id: 00-setup-git
            title: "Mise en place & Git"
            order: 0
            course: cours.md
            groups:
              - id: premiers-commits
                title: "Premiers commits"
                exercises: [ex00-hello, ex01-identite]
              - id: branches-fusion
                title: "Branches & fusion"
                exercises: [ex02-branche]
            """);

        var module = ModuleLoader.Load(dir.Combine("00-setup-git"));

        Assert.Equal("00-setup-git", module.Id);
        Assert.Equal(0, module.Order);
        Assert.Equal("cours.md", module.Course);
        Assert.Equal(2, module.Groups.Count);
        Assert.Equal("premiers-commits", module.Groups[0].Id);
        Assert.Equal(new[] { "ex00-hello", "ex01-identite" }, module.Groups[0].Exercises);
        Assert.Equal(new[] { "ex02-branche" }, module.Groups[1].Exercises);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter ModuleLoaderTests`
Expected : FAIL — « The name 'ModuleLoader' ... could not be found ».

- [ ] **Step 3 : Implémenter `ModuleLoader`**

`src/Piscine.Core/Content/ModuleLoader.cs` :
```csharp
using System.IO;
using Piscine.Core.Io;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Charge le <c>module.yaml</c> d'un dossier de module.</summary>
public static class ModuleLoader
{
    public const string FileName = "module.yaml";

    public static Module Load(string moduleDirectory)
    {
        var path = Path.Combine(moduleDirectory, FileName);
        return YamlLoader.Load<Module>(path);
    }
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter ModuleLoaderTests`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Content/ModuleLoader.cs tests/Piscine.Core.Tests/ModuleLoaderTests.cs
git commit -m "feat(core): ModuleLoader charge module.yaml

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : `ExerciseManifestLoader`

**Files:**
- Test: `tests/Piscine.Core.Tests/ExerciseManifestLoaderTests.cs`
- Create: `src/Piscine.Core/Content/ExerciseManifestLoader.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Core.Tests/ExerciseManifestLoaderTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ExerciseManifestLoaderTests
{
    [Fact]
    public void Load_ParsesManifestStructuralFields()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("ex00-hello", "manifest.yaml"), """
            id: ex00-hello
            title: "Hello, Piscine"
            objective: "Afficher un message precis"
            deliverables: [Hello.cs]
            starter: [starter/README.md]
            """);

        var manifest = ExerciseManifestLoader.Load(dir.Combine("ex00-hello"));

        Assert.Equal("ex00-hello", manifest.Id);
        Assert.Equal("Hello, Piscine", manifest.Title);
        Assert.Equal(new[] { "Hello.cs" }, manifest.Deliverables);
        Assert.Equal(new[] { "starter/README.md" }, manifest.Starter);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter ExerciseManifestLoaderTests`
Expected : FAIL — « The name 'ExerciseManifestLoader' ... could not be found ».

- [ ] **Step 3 : Implémenter `ExerciseManifestLoader`**

`src/Piscine.Core/Content/ExerciseManifestLoader.cs` :
```csharp
using System.IO;
using Piscine.Core.Io;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Charge le <c>manifest.yaml</c> d'un dossier d'exercice.</summary>
public static class ExerciseManifestLoader
{
    public const string FileName = "manifest.yaml";

    public static ExerciseManifest Load(string exerciseDirectory)
    {
        var path = Path.Combine(exerciseDirectory, FileName);
        return YamlLoader.Load<ExerciseManifest>(path);
    }
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter ExerciseManifestLoaderTests`
Expected : PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Content/ExerciseManifestLoader.cs tests/Piscine.Core.Tests/ExerciseManifestLoaderTests.cs
git commit -m "feat(core): ExerciseManifestLoader charge manifest.yaml

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : `ContentDiscovery` (scan + tri par `order`)

**Files:**
- Test: `tests/Piscine.Core.Tests/ContentDiscoveryTests.cs`
- Create: `src/Piscine.Core/Content/ContentDiscovery.cs`

- [ ] **Step 1 : Écrire le test qui échoue** (deux modules créés dans le désordre, on vérifie le tri par `order` et l'ignorance des dossiers sans `module.yaml`)

`tests/Piscine.Core.Tests/ContentDiscoveryTests.cs` :
```csharp
using System.IO;
using System.Linq;
using Piscine.Core;
using Piscine.Core.Content;
using Xunit;

namespace Piscine.Core.Tests;

public class ContentDiscoveryTests
{
    [Fact]
    public void DiscoverModules_ReturnsModulesOrderedByOrder()
    {
        using var dir = new TempDir();
        dir.WriteFile(Path.Combine("modules", "01-bases", "module.yaml"), """
            id: 01-bases
            title: "Bases C#"
            order: 1
            """);
        dir.WriteFile(Path.Combine("modules", "00-setup", "module.yaml"), """
            id: 00-setup
            title: "Setup"
            order: 0
            """);
        // Dossier parasite sans module.yaml : doit être ignoré.
        Directory.CreateDirectory(dir.Combine(Path.Combine("modules", "_brouillon")));

        var paths = new PiscinePaths(dir.Path);
        var modules = ContentDiscovery.DiscoverModules(paths).ToList();

        Assert.Equal(2, modules.Count);
        Assert.Equal("00-setup", modules[0].Id);
        Assert.Equal("01-bases", modules[1].Id);
    }

    [Fact]
    public void DiscoverModules_ReturnsEmptyWhenModulesDirectoryMissing()
    {
        using var dir = new TempDir();
        var paths = new PiscinePaths(dir.Path);

        var modules = ContentDiscovery.DiscoverModules(paths);

        Assert.Empty(modules);
    }
}
```

- [ ] **Step 2 : Lancer le test pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter ContentDiscoveryTests`
Expected : FAIL — « The name 'ContentDiscovery' ... could not be found ».

- [ ] **Step 3 : Implémenter `ContentDiscovery`**

`src/Piscine.Core/Content/ContentDiscovery.cs` :
```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Piscine.Core.Model;

namespace Piscine.Core.Content;

/// <summary>Découvre les modules sous <c>content/modules</c>, triés par <c>order</c>.</summary>
public static class ContentDiscovery
{
    public static IReadOnlyList<Module> DiscoverModules(PiscinePaths paths)
    {
        var modulesDir = paths.ModulesDirectory;
        if (!Directory.Exists(modulesDir))
        {
            return new List<Module>();
        }

        return Directory.EnumerateDirectories(modulesDir)
            .Where(d => File.Exists(Path.Combine(d, ModuleLoader.FileName)))
            .Select(ModuleLoader.Load)
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Id, System.StringComparer.Ordinal)
            .ToList();
    }
}
```

- [ ] **Step 4 : Lancer le test pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter ContentDiscoveryTests`
Expected : PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Content/ContentDiscovery.cs tests/Piscine.Core.Tests/ContentDiscoveryTests.cs
git commit -m "feat(core): ContentDiscovery scanne et trie les modules

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 7 : Modèle de progression (`ExerciseStatus`, `Progress`)

**Files:**
- Create: `src/Piscine.Core/Model/ExerciseStatus.cs`
- Create: `src/Piscine.Core/Model/Progress.cs`

> DTO sans logique → couverts par les tests de `ProgressStore` (Task 8). On vérifie la compilation.

- [ ] **Step 1 : Créer `ExerciseStatus`**

`src/Piscine.Core/Model/ExerciseStatus.cs` :
```csharp
namespace Piscine.Core.Model;

/// <summary>
/// Statut de progression persisté d'un exercice.
/// (« Non corrigé » est un résultat transitoire de la moulinette, non persisté ici.)
/// </summary>
public enum ExerciseStatus
{
    NonCommence,
    ARevoir,
    Reussi
}
```

- [ ] **Step 2 : Créer `Progress` et `ExerciseProgress`**

`src/Piscine.Core/Model/Progress.cs` :
```csharp
using System;
using System.Collections.Generic;

namespace Piscine.Core.Model;

/// <summary>Progression de la recrue : statut par identifiant d'exercice.</summary>
public sealed class Progress
{
    public Dictionary<string, ExerciseProgress> Exercises { get; set; } = new();
}

/// <summary>Progression d'un exercice donné.</summary>
public sealed class ExerciseProgress
{
    public ExerciseStatus Status { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset? LastAttempt { get; set; }
}
```

- [ ] **Step 3 : Vérifier la compilation**

Run: `dotnet build src/Piscine.Core`
Expected : PASS — 0 erreur.

- [ ] **Step 4 : Commit**

```bash
git add src/Piscine.Core/Model/ExerciseStatus.cs src/Piscine.Core/Model/Progress.cs
git commit -m "feat(core): modele de progression (ExerciseStatus, Progress)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 8 : `ProgressStore` (JSON)

**Files:**
- Test: `tests/Piscine.Core.Tests/ProgressStoreTests.cs`
- Create: `src/Piscine.Core/Progress/ProgressStore.cs`

- [ ] **Step 1 : Écrire les tests qui échouent** (round-trip + fichier absent → progression vide)

`tests/Piscine.Core.Tests/ProgressStoreTests.cs` :
```csharp
using System.IO;
using Piscine.Core.Model;
using Piscine.Core.Progress;
using Xunit;

namespace Piscine.Core.Tests;

public class ProgressStoreTests
{
    [Fact]
    public void Load_ReturnsEmptyProgress_WhenFileMissing()
    {
        using var dir = new TempDir();
        var store = new ProgressStore(dir.Combine("progress.json"));

        var progress = store.Load();

        Assert.Empty(progress.Exercises);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsStatusAndAttempts()
    {
        using var dir = new TempDir();
        var path = dir.Combine("progress.json");
        var store = new ProgressStore(path);
        var progress = new Progress();
        progress.Exercises["ex00-hello"] = new ExerciseProgress
        {
            Status = ExerciseStatus.Reussi,
            Attempts = 3
        };

        store.Save(progress);
        var reloaded = new ProgressStore(path).Load();

        Assert.True(File.Exists(path));
        Assert.Equal(ExerciseStatus.Reussi, reloaded.Exercises["ex00-hello"].Status);
        Assert.Equal(3, reloaded.Exercises["ex00-hello"].Attempts);
    }

    [Fact]
    public void Save_CreatesMissingParentDirectory()
    {
        using var dir = new TempDir();
        var path = dir.Combine(Path.Combine("nested", "progress.json"));
        var store = new ProgressStore(path);

        store.Save(new Progress());

        Assert.True(File.Exists(path));
    }
}
```

- [ ] **Step 2 : Lancer les tests pour vérifier l'échec**

Run: `dotnet test tests/Piscine.Core.Tests --filter ProgressStoreTests`
Expected : FAIL — « The name 'ProgressStore' ... could not be found ».

- [ ] **Step 3 : Implémenter `ProgressStore`**

`src/Piscine.Core/Progress/ProgressStore.cs` :
```csharp
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piscine.Core.Model;

namespace Piscine.Core.Progress;

/// <summary>Persiste la progression de la recrue dans un fichier JSON.</summary>
public sealed class ProgressStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public ProgressStore(string path)
    {
        _path = path;
    }

    public Progress Load()
    {
        if (!File.Exists(_path))
        {
            return new Progress();
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<Progress>(json, Options) ?? new Progress();
    }

    public void Save(Progress progress)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(progress, Options);
        File.WriteAllText(_path, json);
    }
}
```

- [ ] **Step 4 : Lancer les tests pour vérifier le succès**

Run: `dotnet test tests/Piscine.Core.Tests --filter ProgressStoreTests`
Expected : PASS (3 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Core/Progress/ProgressStore.cs tests/Piscine.Core.Tests/ProgressStoreTests.cs
git commit -m "feat(core): ProgressStore persiste la progression en JSON

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 9 : Vérification globale + push

**Files:** aucun (vérification)

- [ ] **Step 1 : Lancer toute la suite en Release (comme la CI)**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : PASS — tous les tests verts (4 de l'It.0 + ceux de l'It.1, soit 13 au total).

- [ ] **Step 2 : Pousser sur main et vérifier la CI**

```bash
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success**.

- [ ] **Step 3 : Si la CI échoue**, diagnostiquer puis corriger

```bash
gh run view --log-failed
```
Corriger localement, commit, push, relancer `gh run watch --exit-status` jusqu'au vert.

---

## Self-Review (effectué)

**Couverture spec (It.1) :**
- Parsing `module.yaml` → Task 4 (`ModuleLoader`) + DTO Task 2. ✓
- Parsing `manifest.yaml` (sous-ensemble structurel) → Task 5 + DTO Task 2. ✓ Sections grading/feedback/constraints explicitement reportées à l'It.2 (documenté).
- Découverte de contenu triée par `order` → Task 6 (`ContentDiscovery`), réutilise `PiscinePaths` (It.0). ✓
- Store de progression → Tasks 7-8 (`Progress`, `ProgressStore`). Statuts Réussi/À revoir alignés sur la spec ; « Non corrigé » documenté comme transitoire (It.2). ✓

**Placeholders :** aucun TODO/TBD ; tout le code et toutes les commandes sont complets.

**Cohérence des types :** `Module.Order`/`.Id`/`.Groups`, `ExerciseGroup.Exercises`, `ExerciseManifest.Deliverables`/`.Starter`, `PiscinePaths.ModulesDirectory` (It.0), `YamlLoader.Load<T>(string)`, `ModuleLoader.Load(string)`/`.FileName`, `ExerciseManifestLoader.Load(string)`, `ContentDiscovery.DiscoverModules(PiscinePaths)`, `ProgressStore(string)`/`.Load()`/`.Save(Progress)`, `Progress.Exercises`, `ExerciseProgress.Status`/`.Attempts` : signatures et noms utilisés de façon identique entre tests et implémentations.

**Nullable / WarningsAsErrors :** tous les DTO initialisent strings (`string.Empty`) et collections (`new()`) → pas de CS8618. `Path.GetDirectoryName` géré (null/empty) dans `ProgressStore` et `TempDir`.
