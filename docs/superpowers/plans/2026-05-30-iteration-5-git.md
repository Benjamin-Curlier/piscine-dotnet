# Itération 5 — Git (rendu officiel : `init` + `grade-received`) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Cases `- [ ]` pour le suivi.

**Goal:** Donner à la recrue le **vrai geste de rendu** : `piscine init` met en place un dépôt bare local (« GitLab ») comme `origin` avec un hook `post-receive`, et `git push` déclenche la moulinette `piscine grade-received <sha>` qui checkout le commit reçu, corrige **par groupe / dans l'ordre / stop au 1er KO**, affiche le feedback éducatif et persiste la progression.

**Architecture:** `Piscine.Git` (LibGit2Sharp) gère le bas niveau git (init bare + workspace + remote `origin` + hook) et l'extraction d'un commit. La commande de rendu officiel `GradeReceivedCommand` réutilise tout le moteur existant (`SubmissionLoader`, `GroupGrader`, `ProgressRecorder`, `ResultFormatter`, `Graders.Default`), donc **`Piscine.Git` référence `Piscine.Grading`** (DAG : Git → Grading → Core ; aucun cycle). `Piscine.Cli` route `init`/`grade-received` vers ces composants. Tout est testé sur des dépôts temporaires créés par LibGit2Sharp.

**Tech Stack:** .NET 10, xUnit v2, **LibGit2Sharp 0.31.0** (native binaries bundlés, linux-x64 dispo → CI ubuntu OK).

**Contexte repo (It.0→It.4 faites) :** moteur complet dans `Piscine.Grading` (`Graders.Default()→ExerciseGrader`, `SubmissionLoader.Load(contentDir,wsDir)→ExerciseSubmission`, `GroupGrader(ExerciseGrader).GradeGroup(IEnumerable<ExerciseSubmission>)→IReadOnlyList<ExerciseGradingResult>`, `ProgressRecorder.Apply(Progress,results,now)`, `ResultFormatter.Format(ExerciseGradingResult,FeedbackConfig)→string`, `CommandResult(int ExitCode,string Output)` défini dans `CheckCommand.cs`). `Piscine.Core` : `Module`/`ExerciseGroup`, `ContentDiscovery.DiscoverModules(PiscinePaths)→IReadOnlyList<Module>` (trié par order), `ContentLocator.FindExercise(PiscinePaths,string)→ExerciseLocation?{ModuleId,ExerciseId,ContentDir}`, `PiscineLayout(content,workspace,state)` avec `.Content/.WorkspaceRoot/.StateDir/.ProgressPath/.WorkspaceExerciseDir(mod,ex)`, `ProgressStore(path).Load()/.Save()`. `Piscine.Cli/Program.cs` route déjà `list/start/check/status`. `Piscine.Git` ne contient qu'un `AssemblyMarker`, **pas de projet de tests** (à créer). `tests/Piscine.Grading.Tests` désactive la parallélisation xUnit (Console global) via `AssemblyInfo.cs` — **idem requis pour Git.Tests** (grade-received exécute `IoGrader`). Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| Fichier | Responsabilité |
|---|---|
| `src/Piscine.Git/Piscine.Git.csproj` | (modifié) + LibGit2Sharp + ref Grading |
| `src/Piscine.Core/PiscineLayout.cs` | (modifié) + `RemoteRepoPath` (dépôt bare) |
| `src/Piscine.Git/HookScript.cs` | Génère le contenu du hook `post-receive` (pur) |
| `src/Piscine.Git/GitWorkspace.cs` | `Initialize` : bare + workspace + `origin` + hook |
| `src/Piscine.Git/CommitExtractor.cs` | Matérialise l'arbre d'un commit vers un dossier |
| `src/Piscine.Git/GradeReceivedCommand.cs` | Rendu officiel : extract → groupes → GroupGrader → progression |
| `src/Piscine.Cli/Piscine.Cli.csproj` | (modifié) ref Git |
| `src/Piscine.Cli/Program.cs` | (modifié) commandes `init` / `grade-received` |
| `tests/Piscine.Git.Tests/*` | Nouveau projet xUnit (TempDir + AssemblyInfo + tests) |

Le marqueur `src/Piscine.Git/AssemblyMarker.cs` est supprimé (remplacé par du vrai code).

---

## Task 1 : Projet de tests Git + dépendances + `RemoteRepoPath`

**Files:**
- Create: `tests/Piscine.Git.Tests/` (via `dotnet new xunit`)
- Modify: `src/Piscine.Git/Piscine.Git.csproj`, `Piscine.slnx`
- Modify: `src/Piscine.Core/PiscineLayout.cs`
- Test: `tests/Piscine.Core.Tests/PiscineLayoutTests.cs` (ajout)

- [ ] **Step 1 : Créer le projet de tests + dépendances**

```bash
dotnet new xunit -o tests/Piscine.Git.Tests
dotnet sln Piscine.slnx add tests/Piscine.Git.Tests/Piscine.Git.Tests.csproj
dotnet add tests/Piscine.Git.Tests reference src/Piscine.Git
dotnet add src/Piscine.Git package LibGit2Sharp --version 0.31.0
dotnet add src/Piscine.Git reference src/Piscine.Grading
```

Supprimer `src/Piscine.Git/AssemblyMarker.cs`.

- [ ] **Step 2 : Désactiver la parallélisation + TempDir dans Git.Tests**

`tests/Piscine.Git.Tests/AssemblyInfo.cs` :
```csharp
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
```

`tests/Piscine.Git.Tests/TempDir.cs` : copie conforme de `tests/Piscine.Grading.Tests/TempDir.cs` avec `namespace Piscine.Git.Tests;`.

- [ ] **Step 3 : Écrire le test `RemoteRepoPath` (échec attendu)**

Ajouter à `tests/Piscine.Core.Tests/PiscineLayoutTests.cs` :
```csharp
    [Fact]
    public void RemoteRepoPath_IsUnderStateDir()
    {
        var layout = new PiscineLayout("/c", "/ws", "/home/.state");

        Assert.Equal(System.IO.Path.Combine("/home/.state", "remote.git"), layout.RemoteRepoPath);
    }
```

- [ ] **Step 4 : Implémenter `RemoteRepoPath`**

Ajouter à `src/Piscine.Core/PiscineLayout.cs` (après `ProgressPath`) :
```csharp
    /// <summary>Dépôt bare local servant d'« origin » (le « GitLab » de la piscine).</summary>
    public string RemoteRepoPath => Path.Combine(StateDir, "remote.git");
```

- [ ] **Step 5 : Vérifier build + test ciblé**

Run: `dotnet build tests/Piscine.Git.Tests` → PASS (restore LibGit2Sharp OK).
Run: `dotnet test tests/Piscine.Core.Tests --filter PiscineLayoutTests` → PASS (3 tests).

- [ ] **Step 6 : Commit**

```bash
git add -A
git commit -m "chore(git): projet de tests Piscine.Git + LibGit2Sharp + RemoteRepoPath

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2 : `HookScript` (contenu du hook `post-receive`)

**Files:**
- Create: `src/Piscine.Git/HookScript.cs`
- Test: `tests/Piscine.Git.Tests/HookScriptTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Git.Tests/HookScriptTests.cs` :
```csharp
using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class HookScriptTests
{
    [Fact]
    public void PostReceive_InvokesPiscineGradeReceivedForEachRef()
    {
        var script = HookScript.PostReceive(@"C:\piscine\piscine.exe");

        Assert.StartsWith("#!/bin/sh", script);
        Assert.Contains("while read", script);
        Assert.Contains("grade-received", script);
        Assert.Contains("$newrev", script);
        // Chemin normalisé pour sh (pas d'antislash).
        Assert.Contains("C:/piscine/piscine.exe", script);
        Assert.DoesNotContain("\\", script);
        // Fin de ligne LF uniquement (hook lancé par sh).
        Assert.DoesNotContain("\r", script);
    }
}
```

- [ ] **Step 2 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Git.Tests --filter HookScriptTests` → FAIL (`HookScript` introuvable).

- [ ] **Step 3 : Implémenter `HookScript`**

`src/Piscine.Git/HookScript.cs` :
```csharp
namespace Piscine.Git;

/// <summary>Génère le hook <c>post-receive</c> qui déclenche la moulinette à chaque push.</summary>
public static class HookScript
{
    /// <summary>
    /// Script <c>post-receive</c> : pour chaque référence reçue, appelle
    /// <c>piscine grade-received &lt;newrev&gt;</c>. Le chemin de l'exécutable est
    /// normalisé en slashes pour <c>sh</c> (MinGit sous Windows). Lignes en LF.
    /// </summary>
    public static string PostReceive(string piscineExecutablePath)
    {
        var exe = piscineExecutablePath.Replace('\\', '/');
        return string.Join('\n',
            "#!/bin/sh",
            "while read oldrev newrev refname; do",
            $"  \"{exe}\" grade-received \"$newrev\"",
            "done",
            "");
    }
}
```

- [ ] **Step 4 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Git.Tests --filter HookScriptTests` → PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Git/HookScript.cs tests/Piscine.Git.Tests/HookScriptTests.cs
git commit -m "feat(git): HookScript genere le hook post-receive

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3 : `GitWorkspace.Initialize` (bare + workspace + origin + hook)

**Files:**
- Create: `src/Piscine.Git/GitWorkspace.cs`
- Test: `tests/Piscine.Git.Tests/GitWorkspaceTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Git.Tests/GitWorkspaceTests.cs` :
```csharp
using System.IO;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class GitWorkspaceTests
{
    private static PiscineLayout Layout(TempDir dir) =>
        new(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));

    [Fact]
    public void Initialize_CreatesBareWorkspaceOriginAndHook()
    {
        using var dir = new TempDir();
        var layout = Layout(dir);

        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine");

        Assert.True(Repository.IsValid(layout.WorkspaceRoot));
        Assert.True(Repository.IsValid(layout.RemoteRepoPath));

        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            var origin = repo.Network.Remotes["origin"];
            Assert.NotNull(origin);
        }

        var hook = Path.Combine(layout.RemoteRepoPath, "hooks", "post-receive");
        Assert.True(File.Exists(hook));
        Assert.Contains("grade-received", File.ReadAllText(hook));
    }

    [Fact]
    public void Initialize_IsIdempotent()
    {
        using var dir = new TempDir();
        var layout = Layout(dir);

        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine");
        GitWorkspace.Initialize(layout, "/usr/local/bin/piscine"); // ne doit pas lever

        using var repo = new Repository(layout.WorkspaceRoot);
        Assert.NotNull(repo.Network.Remotes["origin"]);
    }
}
```

- [ ] **Step 2 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Git.Tests --filter GitWorkspaceTests` → FAIL.

- [ ] **Step 3 : Implémenter `GitWorkspace`**

`src/Piscine.Git/GitWorkspace.cs` :
```csharp
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp;
using Piscine.Core;

namespace Piscine.Git;

/// <summary>
/// Met en place l'environnement git de la recrue : un dépôt de travail (workspace),
/// un dépôt bare local servant d'« origin », et le hook <c>post-receive</c> qui
/// déclenche la moulinette à chaque push.
/// </summary>
public static class GitWorkspace
{
    public const string OriginName = "origin";

    public static void Initialize(PiscineLayout layout, string piscineExecutablePath)
    {
        Directory.CreateDirectory(layout.WorkspaceRoot);
        if (!Repository.IsValid(layout.WorkspaceRoot))
        {
            Repository.Init(layout.WorkspaceRoot);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(layout.RemoteRepoPath)!);
        if (!Repository.IsValid(layout.RemoteRepoPath))
        {
            Repository.Init(layout.RemoteRepoPath, isBare: true);
        }

        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            if (repo.Network.Remotes[OriginName] is null)
            {
                repo.Network.Remotes.Add(OriginName, layout.RemoteRepoPath);
            }
        }

        InstallHook(layout.RemoteRepoPath, piscineExecutablePath);
    }

    private static void InstallHook(string bareRepoPath, string piscineExecutablePath)
    {
        var hooksDir = Path.Combine(bareRepoPath, "hooks");
        Directory.CreateDirectory(hooksDir);
        var hookPath = Path.Combine(hooksDir, "post-receive");

        File.WriteAllText(hookPath, HookScript.PostReceive(piscineExecutablePath));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(
                hookPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }
}
```

- [ ] **Step 4 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Git.Tests --filter GitWorkspaceTests` → PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Git/GitWorkspace.cs tests/Piscine.Git.Tests/GitWorkspaceTests.cs
git commit -m "feat(git): GitWorkspace.Initialize met en place bare + workspace + hook

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4 : `CommitExtractor` (matérialiser un commit)

**Files:**
- Create: `src/Piscine.Git/CommitExtractor.cs`
- Test: `tests/Piscine.Git.Tests/CommitExtractorTests.cs`

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Git.Tests/CommitExtractorTests.cs` :
```csharp
using System;
using System.IO;
using LibGit2Sharp;
using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class CommitExtractorTests
{
    [Fact]
    public void Extract_MaterializesNestedTreeToDestination()
    {
        using var dir = new TempDir();
        var repoPath = dir.Combine("repo");
        Repository.Init(repoPath);

        File.WriteAllText(dir.WriteFile(Path.Combine("repo", "00-setup", "ex00", "Hello.cs"), "// code"), "// code");
        File.WriteAllText(dir.WriteFile(Path.Combine("repo", "README.md"), "racine"), "racine");

        string sha;
        using (var repo = new Repository(repoPath))
        {
            Commands.Stage(repo, "*");
            var sig = new Signature("t", "t@t", DateTimeOffset.Now);
            sha = repo.Commit("c", sig, sig).Sha;
        }

        var outDir = dir.Combine("out");
        CommitExtractor.Extract(repoPath, sha, outDir);

        Assert.Equal("// code", File.ReadAllText(Path.Combine(outDir, "00-setup", "ex00", "Hello.cs")));
        Assert.Equal("racine", File.ReadAllText(Path.Combine(outDir, "README.md")));
    }
}
```

> Note : `TempDir.WriteFile` crée déjà le fichier et renvoie le chemin absolu ; le `File.WriteAllText(...)` supplémentaire est redondant mais inoffensif — on garde le retour pour la lisibilité. (Simplifier en `dir.WriteFile(...)` seul est équivalent.)

- [ ] **Step 2 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Git.Tests --filter CommitExtractorTests` → FAIL.

- [ ] **Step 3 : Implémenter `CommitExtractor`**

`src/Piscine.Git/CommitExtractor.cs` :
```csharp
using System;
using System.IO;
using LibGit2Sharp;

namespace Piscine.Git;

/// <summary>Matérialise l'arbre d'un commit (reçu par push) dans un dossier de travail.</summary>
public static class CommitExtractor
{
    public static void Extract(string repoPath, string sha, string destinationDir)
    {
        using var repo = new Repository(repoPath);
        if (repo.Lookup<Commit>(sha) is not { } commit)
        {
            throw new ArgumentException($"Commit introuvable : {sha}", nameof(sha));
        }

        Directory.CreateDirectory(destinationDir);
        WriteTree(commit.Tree, destinationDir);
    }

    private static void WriteTree(Tree tree, string dir)
    {
        Directory.CreateDirectory(dir);
        foreach (var entry in tree)
        {
            var target = Path.Combine(dir, entry.Name);
            switch (entry.TargetType)
            {
                case TreeEntryTargetType.Blob:
                    using (var content = ((Blob)entry.Target).GetContentStream())
                    using (var file = File.Create(target))
                    {
                        content.CopyTo(file);
                    }
                    break;
                case TreeEntryTargetType.Tree:
                    WriteTree((Tree)entry.Target, target);
                    break;
            }
        }
    }
}
```

- [ ] **Step 4 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Git.Tests --filter CommitExtractorTests` → PASS.

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Git/CommitExtractor.cs tests/Piscine.Git.Tests/CommitExtractorTests.cs
git commit -m "feat(git): CommitExtractor materialise l'arbre d'un commit recu

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5 : `GradeReceivedCommand` (rendu officiel de bout en bout)

**Files:**
- Create: `src/Piscine.Git/GradeReceivedCommand.cs`
- Test: `tests/Piscine.Git.Tests/GradeReceivedCommandTests.cs`

Logique : extraire le commit de `layout.RemoteRepoPath` dans un dossier temporaire (snapshot du workspace poussé) ; parcourir les modules (ordre) puis leurs groupes (ordre) ; pour chaque exercice du groupe **présent dans le snapshot** (dossier `<module>/<exo>` existant), assembler une `ExerciseSubmission` via `SubmissionLoader.Load(contentDir, snapshot/<module>/<exo>)` ; corriger le groupe via `GroupGrader` (stop au 1er KO) ; agréger ; appliquer `ProgressRecorder` + `ProgressStore.Save` ; formater chaque résultat via `ResultFormatter`. Exit 0 si aucun `ARevoir`, sinon 1 ; 0 + message si rien à corriger.

- [ ] **Step 1 : Écrire le test qui échoue**

`tests/Piscine.Git.Tests/GradeReceivedCommandTests.cs` :
```csharp
using System;
using System.IO;
using LibGit2Sharp;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Git.Tests;

public class GradeReceivedCommandTests
{
    private static PiscineLayout SetupContent(TempDir dir)
    {
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "module.yaml"), """
            id: 00-setup
            order: 0
            groups:
              - id: g1
                exercises: [ex00]
            """);
        dir.WriteFile(Path.Combine("content", "modules", "00-setup", "exercises", "ex00", "manifest.yaml"), """
            id: ex00
            deliverables: [Hello.cs]
            grading:
              - type: io
                cases:
                  - expect_stdout: "ok"
                    expect_exit: 0
            feedback:
              course_ref: "cours.md#hello"
            """);
        return new PiscineLayout(dir.Combine("content"), dir.Combine("ws"), dir.Combine("state"));
    }

    // Crée le dépôt bare + un commit contenant <module>/<exo>/Hello.cs, renvoie le sha.
    private static string PushSnapshot(PiscineLayout layout, TempDir dir, string helloBody)
    {
        Repository.Init(layout.RemoteRepoPath, isBare: true);
        var workPath = dir.Combine("clone");
        Repository.Clone(layout.RemoteRepoPath, workPath);
        var file = Path.Combine(workPath, "00-setup", "ex00", "Hello.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, helloBody);

        using var repo = new Repository(workPath);
        Commands.Stage(repo, "*");
        var sig = new Signature("recrue", "r@piscine", DateTimeOffset.Now);
        var commit = repo.Commit("rendu", sig, sig);
        repo.Network.Push(repo.Branches[repo.Head.FriendlyName], new PushOptions());
        return commit.Sha;
    }

    [Fact]
    public void Run_Reussi_ReturnsZero_AndRecordsProgress()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"ok\");");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Réussi", result.Output);
        var progress = new ProgressStore(layout.ProgressPath).Load();
        Assert.Equal(ExerciseStatus.Reussi, progress.Exercises["ex00"].Status);
    }

    [Fact]
    public void Run_ARevoir_ReturnsOne_AndShowsCourseRef()
    {
        using var dir = new TempDir();
        var layout = SetupContent(dir);
        var sha = PushSnapshot(layout, dir, "System.Console.Write(\"non\");");

        var result = new GradeReceivedCommand(layout, Graders.Default()).Run(sha);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("À revoir", result.Output);
        Assert.Contains("cours.md#hello", result.Output);
    }
}
```

- [ ] **Step 2 : Lancer (échec attendu)** — Run: `dotnet test tests/Piscine.Git.Tests --filter GradeReceivedCommandTests` → FAIL.

- [ ] **Step 3 : Implémenter `GradeReceivedCommand`**

`src/Piscine.Git/GradeReceivedCommand.cs` :
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Grading;

namespace Piscine.Git;

/// <summary>
/// Rendu officiel : corrige le commit reçu par push, par groupe et dans l'ordre
/// (stop au 1er KO), puis persiste la progression. Réutilise tout le moteur de notation.
/// </summary>
public sealed class GradeReceivedCommand
{
    private readonly PiscineLayout _layout;
    private readonly ExerciseGrader _grader;

    public GradeReceivedCommand(PiscineLayout layout, ExerciseGrader grader)
    {
        _layout = layout;
        _grader = grader;
    }

    public CommandResult Run(string sha)
    {
        var snapshot = Path.Combine(Path.GetTempPath(), "piscine-recu", Guid.NewGuid().ToString("N"));
        try
        {
            CommitExtractor.Extract(_layout.RemoteRepoPath, sha, snapshot);

            var allResults = new List<ExerciseGradingResult>();
            var groupGrader = new GroupGrader(_grader);

            foreach (var module in ContentDiscovery.DiscoverModules(_layout.Content))
            {
                foreach (var group in module.Groups)
                {
                    var submissions = new List<ExerciseSubmission>();
                    foreach (var exerciseId in group.Exercises)
                    {
                        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
                        if (location is null)
                        {
                            continue;
                        }

                        var submittedDir = Path.Combine(snapshot, module.Id, exerciseId);
                        if (!Directory.Exists(submittedDir))
                        {
                            continue; // exercice non rendu dans ce push
                        }

                        submissions.Add(SubmissionLoader.Load(location.ContentDir, submittedDir));
                    }

                    if (submissions.Count > 0)
                    {
                        allResults.AddRange(groupGrader.GradeGroup(submissions));
                    }
                }
            }

            return Persist(allResults);
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    private CommandResult Persist(IReadOnlyList<ExerciseGradingResult> results)
    {
        if (results.Count == 0)
        {
            return new CommandResult(0, "Aucun exercice reconnu dans ce rendu.");
        }

        var store = new ProgressStore(_layout.ProgressPath);
        var progress = store.Load();
        ProgressRecorder.Apply(progress, results, DateTimeOffset.Now);
        store.Save(progress);

        var sb = new StringBuilder();
        var anyToReview = false;
        foreach (var result in results)
        {
            sb.AppendLine(ResultFormatter.Format(result, FeedbackFor(result.ExerciseId)));
            if (result.Status == GraderStatus.ARevoir)
            {
                anyToReview = true;
            }
        }

        return new CommandResult(anyToReview ? 1 : 0, sb.ToString().TrimEnd());
    }

    private FeedbackConfig FeedbackFor(string exerciseId)
    {
        var location = ContentLocator.FindExercise(_layout.Content, exerciseId);
        return location is null
            ? new FeedbackConfig()
            : ExerciseManifestLoader.Load(location.ContentDir).Feedback;
    }

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
```

- [ ] **Step 4 : Lancer (succès)** — Run: `dotnet test tests/Piscine.Git.Tests --filter GradeReceivedCommandTests` → PASS (2 tests).

- [ ] **Step 5 : Commit**

```bash
git add src/Piscine.Git/GradeReceivedCommand.cs tests/Piscine.Git.Tests/GradeReceivedCommandTests.cs
git commit -m "feat(git): GradeReceivedCommand corrige le commit recu par groupe

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6 : Câblage CLI (`init` / `grade-received`) + vérif Release + push

**Files:**
- Modify: `src/Piscine.Cli/Piscine.Cli.csproj` (ref Git)
- Modify: `src/Piscine.Cli/Program.cs`

- [ ] **Step 1 : Référencer `Piscine.Git` depuis la CLI**

```bash
dotnet add src/Piscine.Cli reference src/Piscine.Git
```

- [ ] **Step 2 : Ajouter les commandes dans `Program.cs`**

Ajouter `using Piscine.Git;`. Dans le `switch`, avant `default` :
```csharp
    case "init":
        return Init(layout);

    case "grade-received":
        return GradeReceived(layout, args);
```
Mettre à jour la ligne d'usage du `default` : `list | start <exo> | check <exo> | status | init | grade-received <sha>`.

Ajouter les fonctions locales :
```csharp
static int Init(PiscineLayout layout)
{
    var exe = Environment.ProcessPath ?? "piscine";
    GitWorkspace.Initialize(layout, exe);
    Console.WriteLine("Piscine initialisée.");
    Console.WriteLine($"  workspace : {layout.WorkspaceRoot}");
    Console.WriteLine($"  origin    : {layout.RemoteRepoPath}");
    Console.WriteLine("Travaille dans le workspace, puis : git add/commit/push origin main");
    return 0;
}

static int GradeReceived(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine grade-received <sha>");
        return 64;
    }

    var result = new GradeReceivedCommand(layout, Graders.Default()).Run(args[1]);
    Console.WriteLine(result.Output);
    return result.ExitCode;
}
```

- [ ] **Step 3 : Vérifier compilation + exécution gracieuse**

Run: `dotnet run --project src/Piscine.Cli -- grade-received` → « Usage : piscine grade-received <sha> » (code 64).

> Ne pas lancer `init` en dev sans précaution : il écrirait sous `~/piscine`. Le comportement est couvert par les tests de `GitWorkspace` ; ici on vérifie juste que la commande est routée et compile.

- [ ] **Step 4 : Suite complète en Release**

Run: `dotnet test Piscine.slnx --configuration Release`
Expected : tous verts (Core + Grading + **Git** : 7 nouveaux tests git ; total attendu ≈ 57).

- [ ] **Step 5 : Commit + push + CI**

```bash
git add src/Piscine.Cli/Piscine.Cli.csproj src/Piscine.Cli/Program.cs
git commit -m "feat(cli): commandes init et grade-received (rendu officiel git)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git push origin main
gh run watch --exit-status
```
Expected : run « CI » en **success**. Si échec : `gh run view --log-failed`, corriger, commit, push, re-watch. Point de vigilance CI : LibGit2Sharp doit restaurer ses natives linux-x64 sous ubuntu (inclus dans le paquet) ; `Repository.Identity`/global config absente sur le runner → on fournit toujours une `Signature` explicite (OK dans les tests).

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.5) :** `RemoteRepoPath` (T1) ; contenu du hook `post-receive` (T2) ; `Initialize` bare+workspace+origin+hook, idempotent (T3) ; extraction d'un commit (T4) ; rendu officiel par groupe stop-au-1er-KO + progression (T5) ; câblage CLI `init`/`grade-received` (T6). ✓

**Réutilise :** `SubmissionLoader`, `GroupGrader`, `ProgressRecorder`, `ResultFormatter`, `Graders.Default`, `ContentDiscovery`, `ContentLocator`, `ProgressStore`, `ExerciseManifestLoader`, `CommandResult`. **Nouveau couplage assumé :** `Piscine.Git → Piscine.Grading` (le rendu officiel EST de la notation orchestrée par git) — la spec §3.2 disait « Git dépend de Core » ; à noter dans la mémoire/spec.

**Reporté à l'It.6 (packaging) :** bundling **MinGit** (Windows) et résolution du vrai `piscine` exécutable single-file dans le hook (en dev, `Environment.ProcessPath` pointe le host `dotnet`/`Piscine.Cli` — fonctionnel mais finalisé au packaging) ; doc de mise en œuvre.

**Cohérence des types :** `PiscineLayout.RemoteRepoPath:string` ; `HookScript.PostReceive(string)→string` ; `GitWorkspace.Initialize(PiscineLayout,string)` ; `CommitExtractor.Extract(string,string,string)` ; `GradeReceivedCommand(PiscineLayout,ExerciseGrader).Run(string)→CommandResult`.
