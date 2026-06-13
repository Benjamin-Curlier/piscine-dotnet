using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using Piscine.App.Git;
using Piscine.App.Progress;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Piscine.Grading;

namespace Piscine.App.Tests;

/// <summary>
/// Tests unitaires de <see cref="ProgressService"/> : un cas par statut dérivable.
/// Chaque test construit un état hermétique via TempDir + ProgressStore.Save + workspace
/// + GitFixtureBuilder, exactement comme <see cref="GitStatusServiceTests"/>.
/// </summary>
public sealed class ProgressServiceTests
{
    private static readonly Signature Author =
        new("Test", "test@piscine.dev", new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero));

    private const string ModuleId = "m00";
    private const string ExoId = "ex00";

    // --- Helpers ---------------------------------------------------------------------------------

    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }

    /// <summary>Crée un <see cref="PiscineLayout"/> pointant entièrement dans <paramref name="tmp"/>.</summary>
    private static PiscineLayout Layout(TempDir tmp)
    {
        var workspace = tmp.Combine("workspace");
        var state = tmp.Combine("state");
        Directory.CreateDirectory(workspace);
        Directory.CreateDirectory(state);
        return new PiscineLayout(Path.Combine(RepoRoot, "content"), workspace, state);
    }

    /// <summary>Plante <c>progress.json</c> avec le statut donné pour <see cref="ExoId"/>.</summary>
    private static void PlantProgress(PiscineLayout layout, ExerciseStatus status)
    {
        var progress = new Piscine.Core.Model.Progress();
        progress.Exercises[ExoId] = new ExerciseProgress { Status = status };
        new ProgressStore(layout.ProgressPath).Save(progress);
    }

    /// <summary>Crée un fichier dans le répertoire workspace de l'exercice.</summary>
    private static void PlantWorkspaceFile(PiscineLayout layout)
    {
        var dir = layout.WorkspaceExerciseDir(ModuleId, ExoId);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "solution.cs"), "// wip\n");
    }

    /// <summary>Construit un repo git avec un commit sur main dans <paramref name="workspaceRoot"/>.</summary>
    private static void BuildRepo(string workspaceRoot)
    {
        GitFixtureBuilder.Build(
        [
            new GitFixtureStep
            {
                Branch = "main",
                Message = "initial",
                Files = new Dictionary<string, string> { ["README.md"] = "piscine\n" },
            },
        ], workspaceRoot);
    }

    // --- Tests ------------------------------------------------------------------------------------

    [Fact]
    public void StatusFor_NothingAtAll_ReturnsNonCommence()
    {
        // Arrange : pas de progress.json, pas de workspace, pas de repo git.
        using var tmp = new TempDir();
        var layout = Layout(tmp);
        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert
        Assert.Equal(ExerciseProgressStatus.NonCommence, info.Status);
        Assert.Equal(ModuleId, info.ModuleId);
        Assert.Equal(ExoId, info.ExerciseId);
    }

    [Fact]
    public void StatusFor_WorkspaceFileButNoProgress_ReturnsEnCours()
    {
        // Arrange : fichier workspace présent, pas de progress.json, pas de repo git.
        using var tmp = new TempDir();
        var layout = Layout(tmp);
        PlantWorkspaceFile(layout);
        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert
        Assert.Equal(ExerciseProgressStatus.EnCours, info.Status);
    }

    [Fact]
    public void StatusFor_ProgressARevoir_ReturnsARevoir()
    {
        // Arrange : progress.json planté avec status ARevoir.
        using var tmp = new TempDir();
        var layout = Layout(tmp);
        PlantProgress(layout, ExerciseStatus.ARevoir);
        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert
        Assert.Equal(ExerciseProgressStatus.ARevoir, info.Status);
        Assert.Equal(StatusSource.Progress, info.Source);
    }

    [Fact]
    public void StatusFor_ProgressReussi_RepoAheadOfOriginZero_ReturnsPousseNote()
    {
        // Arrange : progress Reussi + repo avec origin et AheadOfOrigin == 0.
        using var tmp = new TempDir();
        using var originDir = new TempDir();
        var layout = Layout(tmp);

        // Construire le repo dans le workspace.
        BuildRepo(layout.WorkspaceRoot);

        // Créer un bare origin, pousser le commit initial.
        Repository.Init(originDir.Path, isBare: true);
        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            var remote = repo.Network.Remotes.Add("origin", originDir.Path);
            repo.Network.Push(remote, "refs/heads/main:refs/heads/main");
        }

        // Planter progress Reussi.
        PlantProgress(layout, ExerciseStatus.Reussi);

        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert
        Assert.Equal(ExerciseProgressStatus.PousseNote, info.Status);
        Assert.Equal(StatusSource.GitDerived, info.Source);
    }

    [Fact]
    public void StatusFor_ProgressReussi_LocalCommitAhead_ReturnsCommiteNonPousse()
    {
        // Arrange : progress Reussi + repo avec 1 commit local non poussé (AheadOfOrigin == 1).
        using var tmp = new TempDir();
        using var originDir = new TempDir();
        var layout = Layout(tmp);

        // Construire le repo et pousser le commit initial.
        BuildRepo(layout.WorkspaceRoot);
        Repository.Init(originDir.Path, isBare: true);
        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            var remote = repo.Network.Remotes.Add("origin", originDir.Path);
            repo.Network.Push(remote, "refs/heads/main:refs/heads/main");

            // Ajouter un commit local (non poussé) DANS l'exercice m00/ex00 → attribué à cet exo.
            var exoDir = Path.Combine(layout.WorkspaceRoot, ModuleId, ExoId);
            Directory.CreateDirectory(exoDir);
            File.WriteAllText(Path.Combine(exoDir, "extra.txt"), "local\n");
            Commands.Stage(repo, "*");
            repo.Commit("commit local", Author, Author);
        }

        PlantProgress(layout, ExerciseStatus.Reussi);

        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert
        Assert.Equal(ExerciseProgressStatus.CommiteNonPousse, info.Status);
        Assert.Equal(StatusSource.GitDerived, info.Source);
    }

    [Fact]
    public void StatusFor_ProgressReussi_NoOrigin_ReturnsCommiteNonPousse_NotPousseNote()
    {
        // Arrange (dégradation verrouillée) : progress Reussi mais pas d'origin configuré.
        // PousseNote exige HasOrigin — sans origin → CommiteNonPousse.
        using var tmp = new TempDir();
        var layout = Layout(tmp);
        BuildRepo(layout.WorkspaceRoot);
        PlantProgress(layout, ExerciseStatus.Reussi);

        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var info = service.StatusFor(ModuleId, ExoId);

        // Assert : PousseNote NE doit PAS être retourné sans origin.
        Assert.NotEqual(ExerciseProgressStatus.PousseNote, info.Status);
        Assert.Equal(ExerciseProgressStatus.CommiteNonPousse, info.Status);
        Assert.Equal(StatusSource.GitDerived, info.Source);
    }

    [Fact]
    public void StatusFor_CommitOnOneExo_DoesNotContaminateAnotherExo()
    {
        // Arrange (#65) : repo poussé, puis 1 commit local non poussé qui ne touche QUE m00/ex00.
        using var tmp = new TempDir();
        using var originDir = new TempDir();
        var layout = Layout(tmp);
        BuildRepo(layout.WorkspaceRoot);
        Repository.Init(originDir.Path, isBare: true);
        using (var repo = new Repository(layout.WorkspaceRoot))
        {
            var remote = repo.Network.Remotes.Add("origin", originDir.Path);
            repo.Network.Push(remote, "refs/heads/main:refs/heads/main");

            var exoDir = Path.Combine(layout.WorkspaceRoot, ModuleId, ExoId);
            Directory.CreateDirectory(exoDir);
            File.WriteAllText(Path.Combine(exoDir, "Sol.cs"), "// rendu ex00\n");
            Commands.Stage(repo, "*");
            repo.Commit("rendu ex00", Author, Author);
        }

        var service = new ProgressService(layout, new GitStatusService());

        // Act : ex00 (committé non poussé) vs ex01 (jamais touché).
        var ex00 = service.StatusFor(ModuleId, ExoId);
        var ex01 = service.StatusFor(ModuleId, "ex01");

        // Assert : seul ex00 est commité-non-poussé ; ex01 reste NonCommence (pas de contamination).
        Assert.Equal(ExerciseProgressStatus.CommiteNonPousse, ex00.Status);
        Assert.Equal(ExerciseProgressStatus.NonCommence, ex01.Status);
    }

    [Fact]
    public void SnapshotFor_LoadsOnce_ReturnsMappedStatuses()
    {
        // Arrange : deux exos — l'un NonCommence, l'autre ARevoir.
        using var tmp = new TempDir();
        var layout = Layout(tmp);

        var progress = new Piscine.Core.Model.Progress();
        progress.Exercises["ex00"] = new ExerciseProgress { Status = ExerciseStatus.ARevoir };
        new ProgressStore(layout.ProgressPath).Save(progress);

        var service = new ProgressService(layout, new GitStatusService());

        // Act
        var snapshot = service.SnapshotFor(
        [
            ("m00", "ex00"),
            ("m00", "ex01"),
        ]);

        // Assert
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(ExerciseProgressStatus.ARevoir, snapshot[0].Status);
        Assert.Equal(ExerciseProgressStatus.NonCommence, snapshot[1].Status);
    }
}
