using System.IO;
using Piscine.Core.Model;
using Piscine.Core.Progression;
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
