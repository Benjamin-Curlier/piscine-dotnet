using System.IO;
using System.Linq;
using Piscine.App.Checking;
using Piscine.Core;
using Piscine.Grading;

namespace Piscine.App.Tests;

/// <summary>
/// Tests de <see cref="CheckService"/> : correction in-process (without git, sans persistance).
/// Exercice de référence : <c>ex00-hello</c> (module <c>00-setup-git</c>), livrable <c>Hello.cs</c>,
/// 1 cas io attendant <c>"Hello, Piscine!\n"</c>/exit 0,
/// <c>feedback.hints[when=io_mismatch]</c> + <c>course_ref: cours.md#hello-world</c>.
/// </summary>
public sealed class CheckServiceTests
{
    // Résoudre la racine du dépôt via Piscine.slnx (montée depuis le dossier de l'assembly de test).
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string ContentRoot = Path.Combine(RepoRoot, "content");
    private const string ModuleId = "00-setup-git";
    private const string ExerciseId = "ex00-hello";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable depuis l'assembly de test.");
    }

    private static CheckService CreateService(string workspaceRoot, string stateDir) =>
        new CheckService(
            new PiscineLayout(ContentRoot, workspaceRoot, stateDir),
            Graders.Default());

    // --- Known-PASS : copier la solution officielle -----------------------------------------------

    [Fact]
    public void Check_WithCorrectSolution_ReturnsReussi()
    {
        // Arrange
        using var tmp = new TempDir();
        var workspaceExoDir = Path.Combine(tmp.Path, "workspace", ModuleId, ExerciseId);
        Directory.CreateDirectory(workspaceExoDir);

        var solutionFile = Path.Combine(ContentRoot, "modules", ModuleId, "exercises", ExerciseId, "solution", "Hello.cs");
        File.Copy(solutionFile, Path.Combine(workspaceExoDir, "Hello.cs"));

        var service = CreateService(Path.Combine(tmp.Path, "workspace"), Path.Combine(tmp.Path, "state"));

        // Act
        var outcome = service.Check(ExerciseId);

        // Assert
        Assert.Equal(CheckVerdict.Reussi, outcome.Verdict);
        Assert.Equal(ExerciseId, outcome.ExerciseId);
        Assert.Equal(ModuleId, outcome.ModuleId);
        Assert.True(outcome.Cases.All(static c => c.Passed), "Tous les cas doivent être réussis.");
        Assert.Null(outcome.Hint);
        Assert.Null(outcome.CourseRef);
    }

    // --- Known-FAIL : livrable incorrect → io_mismatch -------------------------------------------

    [Fact]
    public void Check_WithWrongOutput_ReturnsARevoir_WithDiffAndHint()
    {
        // Arrange
        using var tmp = new TempDir();
        var workspaceExoDir = Path.Combine(tmp.Path, "workspace", ModuleId, ExerciseId);
        Directory.CreateDirectory(workspaceExoDir);

        // Livrable faux : affiche "Bonjour" au lieu de "Hello, Piscine!"
        File.WriteAllText(Path.Combine(workspaceExoDir, "Hello.cs"), """
            System.Console.WriteLine("Bonjour");
            """);

        var service = CreateService(Path.Combine(tmp.Path, "workspace"), Path.Combine(tmp.Path, "state"));

        // Act
        var outcome = service.Check(ExerciseId);

        // Assert — verdict
        Assert.Equal(CheckVerdict.ARevoir, outcome.Verdict);

        // Assert — au moins un cas io échoué
        var ioCase = outcome.Cases.FirstOrDefault(static c => c.GraderType == "io" && !c.Passed);
        Assert.NotNull(ioCase);

        // Assert — les messages contiennent les lignes Attendu et Obtenu
        Assert.Contains(ioCase.Messages, static m => m.StartsWith("Attendu", System.StringComparison.Ordinal));
        Assert.Contains(ioCase.Messages, static m => m.StartsWith("Obtenu", System.StringComparison.Ordinal));

        // Assert — indice et course_ref issus du manifest
        Assert.NotNull(outcome.Hint);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Hint));
        Assert.Equal("cours.md#hello-world", outcome.CourseRef);
    }

    // --- Introuvable : exercice inexistant -------------------------------------------------------

    [Fact]
    public void Check_WithUnknownExercise_ReturnsIntrouvable()
    {
        // Arrange
        using var tmp = new TempDir();
        var service = CreateService(Path.Combine(tmp.Path, "workspace"), Path.Combine(tmp.Path, "state"));

        // Act
        var outcome = service.Check("ex-qui-nexiste-pas");

        // Assert
        Assert.Equal(CheckVerdict.Introuvable, outcome.Verdict);
        Assert.Empty(outcome.Cases);
        Assert.Null(outcome.Hint);
        Assert.Null(outcome.CourseRef);
    }

    // --- AucunFichier : workspace vide -----------------------------------------------------------

    [Fact]
    public void Check_WithEmptyWorkspace_ReturnsAucunFichier()
    {
        // Arrange : workspace existe mais ne contient pas le livrable Hello.cs
        using var tmp = new TempDir();
        var workspaceExoDir = Path.Combine(tmp.Path, "workspace", ModuleId, ExerciseId);
        Directory.CreateDirectory(workspaceExoDir);

        var service = CreateService(Path.Combine(tmp.Path, "workspace"), Path.Combine(tmp.Path, "state"));

        // Act
        var outcome = service.Check(ExerciseId);

        // Assert
        Assert.Equal(CheckVerdict.AucunFichier, outcome.Verdict);
        Assert.Empty(outcome.Cases);
    }

    // --- Déterminisme : deux appels successifs → même résultat -----------------------------------

    [Fact]
    public void Check_CalledTwice_ReturnsSameVerdict()
    {
        // Arrange
        using var tmp = new TempDir();
        var workspaceExoDir = Path.Combine(tmp.Path, "workspace", ModuleId, ExerciseId);
        Directory.CreateDirectory(workspaceExoDir);

        var solutionFile = Path.Combine(ContentRoot, "modules", ModuleId, "exercises", ExerciseId, "solution", "Hello.cs");
        File.Copy(solutionFile, Path.Combine(workspaceExoDir, "Hello.cs"));

        var service = CreateService(Path.Combine(tmp.Path, "workspace"), Path.Combine(tmp.Path, "state"));

        // Act
        var first = service.Check(ExerciseId);
        var second = service.Check(ExerciseId);

        // Assert
        Assert.Equal(first.Verdict, second.Verdict);
        Assert.Equal(first.Cases.Count, second.Cases.Count);
        Assert.Equal(first.Hint, second.Hint);
        Assert.Equal(first.CourseRef, second.CourseRef);
    }
}
