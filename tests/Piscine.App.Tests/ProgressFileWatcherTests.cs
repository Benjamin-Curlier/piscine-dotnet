using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Piscine.App.Push;
using Piscine.Core;
using Piscine.Core.Progression;
using CoreProgress = Piscine.Core.Model.Progress;
using ExerciseProgress = Piscine.Core.Model.ExerciseProgress;
using ExerciseStatus = Piscine.Core.Model.ExerciseStatus;

namespace Piscine.App.Tests;

/// <summary>
/// Tests unitaires de <see cref="ProgressFileWatcher"/> : détection d'écriture, delta seul,
/// pas de faux positif, debounce, mapping Reussi, et dispose propre.
/// </summary>
public sealed class ProgressFileWatcherTests : IAsyncLifetime
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }

    private readonly TempDir _temp = new();

    // Chaque test obtient un layout isolé dans son propre TempDir.
    private PiscineLayout CreateLayout()
    {
        var state = _temp.Combine(".state");
        var workspace = _temp.Combine("workspace");
        // PiscineLayout(contentRoot, workspaceRoot, stateDir)
        return new PiscineLayout(Path.Combine(RepoRoot, "content"), workspace, state);
    }

    /// <summary>Écrit <c>progress.json</c> via <see cref="ProgressStore"/> (format garanti = même API que le hook).</summary>
    private static void WriteProgress(PiscineLayout layout, params (string Id, ExerciseStatus Status, int Attempts)[] entries)
    {
        var progress = new CoreProgress();
        foreach (var (id, status, attempts) in entries)
        {
            progress.Exercises[id] = new ExerciseProgress
            {
                Status = status,
                Attempts = attempts,
                LastAttempt = attempts > 0 ? DateTimeOffset.UtcNow : null,
            };
        }
        new ProgressStore(layout.ProgressPath).Save(progress);
    }

    private const int EventTimeoutMs = 5_000;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // TempDir.Dispose est synchrone ; on l'appelle ici pour cohérence IAsyncLifetime.
        await Task.Run(() => _temp.Dispose());
    }

    // ── Test 1 : Détection d'une écriture ────────────────────────────────────

    [Fact]
    public async Task Start_ThenWriteProgress_FiresResultReceived_WithExpectedEntry()
    {
        // Arrange
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var tcs = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += r => tcs.TrySetResult(r);

        // Act — démarrer (aucun progress.json → snapshot vide) puis écrire.
        watcher.Start();
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.ARevoir, 1));

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(EventTimeoutMs));
        Assert.True(completed == tcs.Task, "ResultReceived non déclenché dans le délai imparti.");

        var result = await tcs.Task;
        Assert.Single(result.Changed);
        var entry = result.Changed[0];
        Assert.Equal("ex00-hello", entry.ExerciseId);
        Assert.Equal(PushVerdict.ARevoir, entry.Verdict);
        Assert.Equal(1, entry.Attempts);

        // LatestResult() doit retourner le même résultat.
        var latest = watcher.LatestResult();
        Assert.NotNull(latest);
        Assert.Equal("ex00-hello", latest.Changed[0].ExerciseId);
    }

    // ── Test 2 : Delta seul ───────────────────────────────────────────────────

    [Fact]
    public async Task Start_WithExistingProgress_OnlyNewExerciseInEvent()
    {
        // Arrange — pré-écrire AVANT Start() (absorbé dans le snapshot initial).
        var layout = CreateLayout();
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.Reussi, 1));

        await using var watcher = new ProgressFileWatcher(layout);
        var tcs = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += r => tcs.TrySetResult(r);

        // Act — démarrer après la pré-écriture, puis ajouter un nouvel exercice.
        watcher.Start();
        WriteProgress(layout,
            ("ex00-hello", ExerciseStatus.Reussi, 1),   // inchangé
            ("ex01-foo", ExerciseStatus.ARevoir, 1));    // nouveau

        // Assert — uniquement ex01-foo dans le delta.
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(EventTimeoutMs));
        Assert.True(completed == tcs.Task, "ResultReceived non déclenché dans le délai imparti.");

        var result = await tcs.Task;
        Assert.Single(result.Changed);
        Assert.Equal("ex01-foo", result.Changed[0].ExerciseId);
    }

    // ── Test 3 : Pas de faux positif ─────────────────────────────────────────

    [Fact]
    public async Task Start_ThenRewriteSameContent_NoEventFired()
    {
        // Arrange
        var layout = CreateLayout();
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.ARevoir, 1));

        await using var watcher = new ProgressFileWatcher(layout);
        int eventCount = 0;
        watcher.ResultReceived += _ => Interlocked.Increment(ref eventCount);

        watcher.Start();

        // Act — ré-écrire exactement le même contenu.
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.ARevoir, 1));

        // Assert — attendre plus que le debounce pour s'assurer qu'aucun événement n'est parti.
        await Task.Delay(600);
        Assert.Equal(0, Volatile.Read(ref eventCount));
    }

    // ── Test 4 : Debounce ─────────────────────────────────────────────────────

    [Fact]
    public async Task Start_FiveRapidSaves_SingleEventWithLastState()
    {
        // Arrange
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var received = new List<PushResult>();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += r =>
        {
            received.Add(r);
            tcs.TrySetResult(true);
        };

        watcher.Start();

        // Act — 5 sauvegardes rapprochées (intervalles < debounce de 250 ms).
        for (int i = 1; i <= 5; i++)
        {
            WriteProgress(layout, ("ex00-hello", ExerciseStatus.ARevoir, i));
            await Task.Delay(30); // rapide mais pas nul
        }

        // Assert — attendre la fin du debounce + marge.
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(EventTimeoutMs));
        Assert.True(completed == tcs.Task, "ResultReceived non déclenché dans le délai imparti.");

        // Laisser un peu plus de temps pour d'éventuels événements supplémentaires.
        await Task.Delay(400);

        Assert.Single(received);
        // Le dernier état (Attempts=5) doit être dans l'événement.
        Assert.Equal(5, received[0].Changed[0].Attempts);
    }

    // ── Test 5 : Mapping Reussi ───────────────────────────────────────────────

    [Fact]
    public async Task Start_ThenWriteReussi_VerdictIsReussiWithCorrectAttempts()
    {
        // Arrange
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var tcs = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += r => tcs.TrySetResult(r);

        watcher.Start();

        // Act
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.Reussi, 2));

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(EventTimeoutMs));
        Assert.True(completed == tcs.Task, "ResultReceived non déclenché dans le délai imparti.");

        var r2 = await tcs.Task;
        var entry = r2.Changed[0];
        Assert.Equal(PushVerdict.Reussi, entry.Verdict);
        Assert.Equal(2, entry.Attempts);
        Assert.NotNull(entry.LastAttempt);
    }

    // ── Test 6 : Dispose ─────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_ThenWrite_NoEventFired()
    {
        // Arrange
        var layout = CreateLayout();
        var watcher = new ProgressFileWatcher(layout);
        int eventCount = 0;
        watcher.ResultReceived += _ => Interlocked.Increment(ref eventCount);
        watcher.Start();

        // Act — disposer avant d'écrire.
        await watcher.DisposeAsync();
        WriteProgress(layout, ("ex00-hello", ExerciseStatus.ARevoir, 1));

        // Assert — attendre plus que le debounce.
        await Task.Delay(600);
        Assert.Equal(0, Volatile.Read(ref eventCount));
        // TempDir doit se nettoyer sans IOException.
    }

    // ── Test 7 : Résultat riche absent → null (rétro-compat statut-only) ───────

    [Fact]
    public void LatestRichResult_WhenArtifactAbsent_ReturnsNull()
    {
        var layout = CreateLayout();
        var watcher = new ProgressFileWatcher(layout);
        Assert.Null(watcher.LatestRichResult());
    }

    // ── Test 8 : Résultat riche présent → document lu (diff/indice/cours) ──────

    [Fact]
    public void LatestRichResult_AfterArtifactWritten_ReturnsDocument()
    {
        var layout = CreateLayout();
        var doc = new PushResultDocument(
            new[]
            {
                new PushExerciseResult(
                    "ex00-hello", "00-setup", "ARevoir",
                    new[] { new PushCaseResult("io", false, new[] { "Attendu : ok", "Obtenu  : non" }) },
                    Hint: "Relis l'énoncé.",
                    CourseRef: "cours.md#hello"),
            },
            DateTimeOffset.UtcNow);
        new LastPushResultStore(layout.LastPushResultPath).Save(doc);

        var loaded = new ProgressFileWatcher(layout).LatestRichResult();

        Assert.NotNull(loaded);
        var ex = Assert.Single(loaded!.Exercises);
        Assert.Equal("ex00-hello", ex.ExerciseId);
        Assert.Equal("ARevoir", ex.Status);
        Assert.Equal("cours.md#hello", ex.CourseRef);
        Assert.Contains(ex.Cases, c => c.GraderType == "io" && !c.Passed);
    }
}
