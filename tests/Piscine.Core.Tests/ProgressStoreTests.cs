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

    [Fact]
    public void Load_ReturnsEmptyProgress_WhenJsonCorrupt()
    {
        using var dir = new TempDir();
        var path = dir.Combine("progress.json");
        // Fichier tronqué/édité à la main : Load() ne doit pas planter (sinon `check` et le hook
        // post-receive crasheraient), mais repartir d'une progression vide.
        File.WriteAllText(path, "{ this is not valid json ");

        var progress = new ProgressStore(path).Load();

        Assert.Empty(progress.Exercises);
    }

    [Fact]
    public void Save_LeavesNoTempFile_AndOverwritesAtomically()
    {
        using var dir = new TempDir();
        var path = dir.Combine("progress.json");
        var store = new ProgressStore(path);

        store.Save(new Progress());
        store.Save(new Progress()); // un second Save (la cible existe déjà) doit réussir via Move overwrite

        Assert.True(File.Exists(path));
        // Le temporaire porte un nom UNIQUE (GUID) par écriture, mais aucun ne doit subsister : le
        // try/finally supprime tout .tmp orphelin (échec de Move) — sinon les collisions concurrentes
        // laisseraient des débris.
        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp"));
    }

    [Fact]
    public void Save_UsesUniqueTempNamePerWrite_LeavingNoOrphan()
    {
        using var dir = new TempDir();
        var path = dir.Combine("progress.json");
        var store = new ProgressStore(path);

        // Deux Save consécutifs : chacun écrit dans un temporaire distinct (nom à base de GUID) puis
        // Move par-dessus la cible. Aucun .tmp ne doit rester — c'est ce qui évite qu'un hook
        // grade-received et un `check` concurrents se disputent un « progress.json.tmp » partagé.
        store.Save(new Progress());
        store.Save(new Progress());

        Assert.True(File.Exists(path));
        Assert.Empty(Directory.GetFiles(dir.Path, "*.tmp"));
    }
}
