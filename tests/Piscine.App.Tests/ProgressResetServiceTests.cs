using Piscine.App.Progress;
using Piscine.Core;
using Piscine.Core.Model;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.App.Tests;

public sealed class ProgressResetServiceTests : IDisposable
{
    private readonly string _home;
    private readonly PiscineLayout _layout;
    private readonly ProgressStore _store;

    public ProgressResetServiceTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "piscine-reset-" + Guid.NewGuid().ToString("N"));
        var state = Path.Combine(_home, ".state");
        Directory.CreateDirectory(state);
        _layout = new PiscineLayout(Path.Combine(_home, "content"), Path.Combine(_home, "workspace"), state);
        _store = new ProgressStore(_layout.ProgressPath);
    }

    private void Seed(params string[] ids)
    {
        var progress = new Core.Model.Progress();
        foreach (var id in ids)
        {
            progress.Exercises[id] = new ExerciseProgress { Status = ExerciseStatus.Reussi, Attempts = 1 };
        }

        _store.Save(progress);
    }

    [Fact]
    public void ResetAll_clears_all_progress()
    {
        Seed("ex00-hello", "ex01-a", "ex01-b");

        new ProgressResetService(_layout).ResetAll();

        Assert.Empty(_store.Load().Exercises);
    }

    [Fact]
    public void ResetExercises_removes_only_given_ids_and_returns_count()
    {
        Seed("ex00-hello", "ex01-a", "ex01-b");

        var removed = new ProgressResetService(_layout).ResetExercises(["ex01-a", "ex01-b", "absent"]);

        Assert.Equal(2, removed);
        var remaining = _store.Load().Exercises;
        Assert.True(remaining.ContainsKey("ex00-hello"));
        Assert.False(remaining.ContainsKey("ex01-a"));
        Assert.False(remaining.ContainsKey("ex01-b"));
    }

    [Fact]
    public void ResetExercises_with_no_match_is_noop()
    {
        Seed("ex00-hello");

        var removed = new ProgressResetService(_layout).ResetExercises(["absent"]);

        Assert.Equal(0, removed);
        Assert.Single(_store.Load().Exercises);
    }

    public void Dispose()
    {
        try { Directory.Delete(_home, recursive: true); } catch { /* best-effort */ }
    }
}
