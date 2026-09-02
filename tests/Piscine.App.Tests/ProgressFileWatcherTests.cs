using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Piscine.App.Push;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using CoreProgress = Piscine.Core.Model.Progress;

namespace Piscine.App.Tests;

/// <summary>
/// Vérifie que seules les écritures de l'artefact <c>last-push-result.json</c> déclenchent un
/// résultat de push et que le résultat simple reste corrélé au document riche.
/// </summary>
public sealed class ProgressFileWatcherTests : IAsyncLifetime
{
    private static readonly string RepoRoot = FindRepoRoot();
    private const int EventTimeoutMs = 5_000;
    private readonly TempDir _temp = new();

    private PiscineLayout CreateLayout() => new(
        Path.Combine(RepoRoot, "content"),
        _temp.Combine("workspace"),
        _temp.Combine(".state"));

    private static PushResultDocument WritePush(
        PiscineLayout layout,
        string id,
        string status,
        int attempts,
        string pushId)
    {
        var attemptedAt = DateTimeOffset.UtcNow;
        var document = new PushResultDocument(
            new[]
            {
                new PushExerciseResult(
                    id,
                    "00-setup",
                    status,
                    new[] { new PushCaseResult("io", status == "Reussi", new[] { "verdict" }) },
                    Hint: null,
                    CourseRef: null)
                {
                    Attempts = attempts,
                    LastAttempt = attemptedAt,
                },
            },
            attemptedAt)
        {
            PushId = pushId,
        };
        new LastPushResultStore(layout.LastPushResultPath).Save(document);
        return document;
    }

    [Fact]
    public async Task Start_ThenWritePushArtifact_FiresCorrelatedResult()
    {
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var received = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += result => received.TrySetResult(result);

        watcher.Start();
        var written = WritePush(layout, "ex00-hello", "ARevoir", 3, "push-1");

        var result = await AwaitResult(received);
        var entry = Assert.Single(result.Changed);
        Assert.Equal("ex00-hello", entry.ExerciseId);
        Assert.Equal(PushVerdict.ARevoir, entry.Verdict);
        Assert.Equal(3, entry.Attempts);
        Assert.Equal(written.Exercises[0].LastAttempt, entry.LastAttempt);
        Assert.Same(result, watcher.LatestResult());
        Assert.Equal("push-1", watcher.LatestRichResult()!.PushId);
    }

    [Fact]
    public async Task ProgressOnlyWrite_DoesNotPretendToBeAPush()
    {
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var eventCount = 0;
        watcher.ResultReceived += _ => Interlocked.Increment(ref eventCount);
        watcher.Start();

        var progress = new CoreProgress();
        progress.Exercises["ex00-hello"] = new ExerciseProgress
        {
            Status = ExerciseStatus.ARevoir,
            Attempts = 1,
        };
        new ProgressStore(layout.ProgressPath).Save(progress);

        await Task.Delay(700, TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref eventCount));
        Assert.Null(watcher.LatestResult());
    }

    [Fact]
    public async Task ExistingArtifact_IsAbsorbed_ThenNewPushIsPublished()
    {
        var layout = CreateLayout();
        WritePush(layout, "ex00", "Reussi", 1, "old-push");
        await using var watcher = new ProgressFileWatcher(layout);
        var received = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += result => received.TrySetResult(result);

        watcher.Start();
        WritePush(layout, "ex01", "ARevoir", 1, "new-push");

        var result = await AwaitResult(received);
        Assert.Equal("ex01", Assert.Single(result.Changed).ExerciseId);
    }

    [Fact]
    public async Task RewritingSamePushId_DoesNotPublishTwice()
    {
        var layout = CreateLayout();
        var document = WritePush(layout, "ex00", "ARevoir", 1, "same-push");
        await using var watcher = new ProgressFileWatcher(layout);
        var eventCount = 0;
        watcher.ResultReceived += _ => Interlocked.Increment(ref eventCount);
        watcher.Start();

        new LastPushResultStore(layout.LastPushResultPath).Save(document);

        await Task.Delay(700, TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref eventCount));
    }

    [Fact]
    public async Task FiveRapidArtifacts_PublishOnlyLastCorrelatedDocument()
    {
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var received = new List<PushResult>();
        var signal = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += result =>
        {
            lock (received)
            {
                received.Add(result);
            }
            signal.TrySetResult(result);
        };
        watcher.Start();

        for (var index = 1; index <= 5; index++)
        {
            WritePush(layout, "ex00", "ARevoir", index, $"push-{index}");
            await Task.Delay(30, TestContext.Current.CancellationToken);
        }

        var result = await AwaitResult(signal);
        await Task.Delay(400, TestContext.Current.CancellationToken);
        Assert.Equal(5, Assert.Single(result.Changed).Attempts);
        lock (received)
        {
            Assert.Single(received);
        }
    }

    [Theory]
    [InlineData("Reussi", PushVerdict.Reussi)]
    [InlineData("EnAttenteRelecture", PushVerdict.EnAttenteRelecture)]
    public async Task RichStatus_IsMappedWithoutConsultingProgress(string status, PushVerdict expected)
    {
        var layout = CreateLayout();
        await using var watcher = new ProgressFileWatcher(layout);
        var received = new TaskCompletionSource<PushResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.ResultReceived += result => received.TrySetResult(result);
        watcher.Start();

        WritePush(layout, "ex00", status, 2, $"push-{status}");

        Assert.Equal(expected, Assert.Single((await AwaitResult(received)).Changed).Verdict);
    }

    [Fact]
    public async Task DisposeAsync_ThenWriteArtifact_DoesNotPublish()
    {
        var layout = CreateLayout();
        var watcher = new ProgressFileWatcher(layout);
        var eventCount = 0;
        watcher.ResultReceived += _ => Interlocked.Increment(ref eventCount);
        watcher.Start();
        await watcher.DisposeAsync();

        WritePush(layout, "ex00", "ARevoir", 1, "after-dispose");

        await Task.Delay(700, TestContext.Current.CancellationToken);
        Assert.Equal(0, Volatile.Read(ref eventCount));
    }

    [Fact]
    public async Task Start_WhenArtifactLocked_DoesNotThrow()
    {
        var layout = CreateLayout();
        WritePush(layout, "ex00", "ARevoir", 1, "locked");
        using var locked = new FileStream(
            layout.LastPushResultPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None);
        await using var watcher = new ProgressFileWatcher(layout);

        Assert.Null(Record.Exception(watcher.Start));
    }

    [Fact]
    public void LatestRichResult_LoadsArtifactBeforeStart()
    {
        var layout = CreateLayout();
        WritePush(layout, "ex00", "ARevoir", 1, "read-directly");

        var loaded = new ProgressFileWatcher(layout).LatestRichResult();

        Assert.Equal("read-directly", loaded!.PushId);
        Assert.Equal("ex00", Assert.Single(loaded.Exercises).ExerciseId);
    }

    private static async Task<PushResult> AwaitResult(TaskCompletionSource<PushResult> source)
    {
        var completed = await Task.WhenAny(
            source.Task,
            Task.Delay(EventTimeoutMs, TestContext.Current.CancellationToken));
        Assert.Same(source.Task, completed);
        return await source.Task;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Piscine.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await Task.Run(_temp.Dispose);
}
